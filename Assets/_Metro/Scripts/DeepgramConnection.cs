using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

using NativeWebSocket;

[System.Serializable]
public class DeepgramResponse
{
    public int[] channel_index;
    public bool is_final;
    public Channel channel;
}

[System.Serializable]
public class Channel
{
    public Alternative[] alternatives;
}

[System.Serializable]
public class Alternative
{
    public string transcript;
}


public class DeepgramConnection : MonoBehaviour
{
    AudioSource _audioSource;
    int lastPos, curPos;

    private WebSocket ws;

    public bool start = false, stop = false;
    [SerializeField] string API_Key;
    private bool playing = false;
    private bool shouldStop = false;

    private string command = "";




    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        Debug.Assert(_audioSource != null, "DeepgramConnection missing Audio Source");
    }
    // Start is called before the first frame update
    void Start()
    {
        //Connect to Mic
        Debug.Log("Connecting to mic");
        Debug.Assert(Microphone.devices.Length > 0, "No microphone available for DeepgramConnection");
        _audioSource.clip = Microphone.Start(null, true, 30, AudioSettings.outputSampleRate);
        _audioSource.Play();
        curPos = 0; lastPos = 0;
        SetupWebsocket();

    }

    IEnumerator StartRecording()
    {
        if (playing || shouldStop)
            yield return null;

        playing = true;
        shouldStop = false;
        yield return new WaitForSeconds(29);
        if (playing && !shouldStop)
        {
            StartCoroutine(FinishRecording());
        }
    }

    IEnumerator FinishRecording()
    {
        if (!playing || shouldStop)
            yield return null;


        shouldStop = true;
        while (shouldStop)
        {
            yield return new WaitForEndOfFrame();
        }
        MetroManager.AddInstructions(command);
        Debug.Log("Finish command: " + command);
        command = "";
    }

    void ProcessAudio()
    {
        curPos = Microphone.GetPosition(null);
        if (curPos > 0)
        {
            if (lastPos > curPos)
            {
                lastPos = 0;
            }
            if (curPos - lastPos > 0)
            {
                int numSamples = (curPos - lastPos) * _audioSource.clip.channels;
                float[] samples = new float[numSamples];
                _audioSource.clip.GetData(samples, lastPos);

                //Convert to byte[] for deepgram
                short[] samplesAsShorts = new short[numSamples];
                for (int i = 0; i < numSamples; i++)
                {
                    samplesAsShorts[i] = f32_to_i16(samples[i]);
                }
                var samplesAsBytes = new byte[numSamples * 2];
                System.Buffer.BlockCopy(samplesAsShorts, 0, samplesAsBytes, 0, samplesAsBytes.Length);
                SendToDeepgram(samplesAsBytes);
                lastPos = curPos;
            }
        }
    }

    async void SetupWebsocket()
    {
        var headers = new Dictionary<string, string>{
            { "Authorization", $"Token {API_Key}" }
        };
        ws = new WebSocket(
                "wss://api.deepgram.com/v1/listen?encoding=linear16&sample_rate=" +
                AudioSettings.outputSampleRate.ToString(),
                headers);

        ws.OnOpen += () =>
        {
            Debug.Log("Connected to Deepgram");
        };

        ws.OnError += (e) =>
        {
            Debug.LogError(e);
        };

        ws.OnClose += (e) =>
        {
            Debug.LogError(e);
            ws = null;
        };

        ws.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);

            //unpack deepgram response
            DeepgramResponse deepgramResponse = new DeepgramResponse();
            object boxedDeepgramResponse = deepgramResponse;
            JsonUtility.FromJsonOverwrite(message, boxedDeepgramResponse);
            deepgramResponse = (DeepgramResponse)boxedDeepgramResponse;
            if (deepgramResponse.is_final)
            {
                var transcript = deepgramResponse.channel.alternatives[0].transcript;
                if (playing)
                {
                    command += transcript;
                    Debug.Log("Command: " + command);
                    if (shouldStop)
                    {
                        playing = false;
                        shouldStop = false;
                    }
                }
            }
        };

        await ws.Connect();
    }

    async void SendToDeepgram(byte[] data)
    {
        if (ws.State == WebSocketState.Open)
        {
            await ws.Send(data);
        }
    }

    short f32_to_i16(float f)
    {
        f = f * 32768;
        if (f > 32767)
        {
            return 32767;
        }
        if (f < -32768)
        {
            return -32768;
        }
        return (short)f;
    }


    // Update is called once per frame
    void Update()
    {
        if (start)
        {
            start = false;
            Debug.Log("start pressed");
            StartCoroutine(StartRecording());
        }
        if (stop)
        {
            stop = false;
            Debug.Log("stop pressed");
            StartCoroutine(FinishRecording());
        }

        ProcessAudio();

        if (ws != null)
        {
            if (ws.State == WebSocketState.Open)
            {
                ws.DispatchMessageQueue();
            }
        }

    }
}
