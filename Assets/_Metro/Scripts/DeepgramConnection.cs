using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// TTS IMPORTS
using RestSharp;
using System.IO;

using UnityEngine.Networking;
using NativeWebSocket;
using System.Text;
using System;
using System.Threading.Tasks;


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
    public AudioSource microphoneSource;
    public AudioSource ttsSource;

    AudioSource _audioSource;
    int lastPos, curPos;

    private WebSocket ws;

    public bool start = false, stop = false;

    private string API_Key;
    private bool hasKey = false;
    private bool playing = false;
    private bool shouldStop = false;

    private string command = "";

    //TODO: debug
    public string testString = "";
    public bool test = false;

    private byte[] ttsAudioBytes = null;
    private object ttsLock = new object();

    private bool isReconnecting = false;

    void Awake()
    {
        if (microphoneSource == null)
        {
            microphoneSource = gameObject.AddComponent<AudioSource>();
        }
        if (ttsSource == null)
        {
            ttsSource = gameObject.AddComponent<AudioSource>();
        }
        // _audioSource = GetComponent<AudioSource>();
        // Debug.Assert(_audioSource != null, "DeepgramConnection missing Audio Source");
        MetroManager.Instance.deepgramConnection = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        try
        {
            API_Key = DeepgramKey.Value;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Deepgram API Key not found: " + e.Message);
            return;
        }
        hasKey = true;
        //Connect to Mic
        Debug.Log("Connecting to mic");
        Debug.Assert(Microphone.devices.Length > 0, "No microphone available for DeepgramConnection");
        microphoneSource.clip = Microphone.Start(null, true, 30, AudioSettings.outputSampleRate);
        microphoneSource.loop = true;
        microphoneSource.Play();
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
                int numSamples = (curPos - lastPos) * microphoneSource.clip.channels;
                float[] samples = new float[numSamples];
                microphoneSource.clip.GetData(samples, lastPos);

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

    async Task CleanupWebsocketAsync()
    {
        if (ws == null) return;

        // detach handlers
        ws.OnOpen -= OnOpenHandler;
        ws.OnError -= OnErrorHandler;
        ws.OnClose -= OnCloseHandler;
        ws.OnMessage -= OnDeepgramMessage;

        // only close if open or connecting
        if (ws.State == WebSocketState.Open || ws.State == WebSocketState.Connecting)
        {
            try
            {
                await ws.Close();   // await the async close
            }
            catch (Exception ex)
            {
                Debug.LogWarning("WebSocket close failed: " + ex.Message);
            }
        }

        ws = null;
    }

    void OnOpenHandler() { Debug.Log("Connected to Deepgram"); StartCoroutine(KeepAliveLoop()); }
    void OnErrorHandler(string e) { Debug.LogError("WebSocket error: " + e); TryReconnect(); }
    void OnCloseHandler(WebSocketCloseCode e) { Debug.LogError("WebSocket closed: " + e); TryReconnect(); }

    IEnumerator KeepAliveLoop()
    {
        const string json = "{\"type\":\"KeepAlive\"}";
        while (ws != null && ws.State == WebSocketState.Open)
        {
            ws.SendText(json);
            yield return new WaitForSeconds(3);
        }
    }

    async void SetupWebsocket()
    {
        await CleanupWebsocketAsync();

        var headers = new Dictionary<string, string> { { "Authorization", $"Token {API_Key}" } };
        ws = new WebSocket(
            "wss://api.deepgram.com/v1/listen?encoding=linear16&sample_rate=" +
            AudioSettings.outputSampleRate,
            headers);

        ws.OnOpen += OnOpenHandler;
        ws.OnError += OnErrorHandler;
        ws.OnClose += OnCloseHandler;
        ws.OnMessage += OnDeepgramMessage;

        await ws.Connect();
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        var _ = CleanupWebsocketAsync();
    }

    // Schedule a reconnect after a short delay, but only one at a time
    void TryReconnect()
    {
        if (isReconnecting) return;
        isReconnecting = true;
        StartCoroutine(ReconnectCoroutine());
    }

    IEnumerator ReconnectCoroutine()
    {
        yield return new WaitForSeconds(2);          // back-off delay
        if (ws != null && ws.State != WebSocketState.Open)
        {
            Debug.Log("Attempting WebSocket reconnect...");
            SetupWebsocket();                        // re-create connection
        }
        isReconnecting = false;
    }

    void OnDeepgramMessage(byte[] bytes)
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
                command += " " + transcript;
                Debug.Log("Command: " + command);
                if (shouldStop)
                {
                    playing = false;
                    shouldStop = false;
                }
            }
        }
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

    public void StartDeepgram()
    {
        start = true;
    }

    public void StopDeepgram()
    {
        stop = true;
    }

    public void Speak(string text)
    {
    public void Speak(string text)
    {

        Debug.Log("[DeepgramConnection] Testing Speak for " + text);
        var client = new RestClient("https://api.deepgram.com/v1/speak?model=aura-asteria-en");


        var request = new RestRequest("/", Method.Post);
        request.AddHeader("Authorization", $"Token {API_Key}");
        request.AddHeader("Content-Type", "application/json");

        request.AddParameter("application/json", $"{{\n  \"text\": \"{text}\"\n}}", ParameterType.RequestBody);
        var response = client.Execute(request);

        Debug.Log("[DeepgramConnection] Send request for " + text);

        if (response.IsSuccessful && response.RawBytes != null)
        {
            if (response.IsSuccessful && response.RawBytes != null)
            {
                Debug.Log("[DeepgramConnection] Response successful for " + text);

                lock (ttsLock)
                {
                    ttsAudioBytes = response.RawBytes;
                }


            }
            else
            {
                Debug.Log("[DeepgramConnection] Response not successful for" + text);
                Debug.LogError("Error in TTS request: " + response.StatusCode);
                Debug.LogError(response.Content);
            }
        }


        // Update is called once per frame
        void Update()
        {
            if (!hasKey)
                return;
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

            if (playing)
            {
                ProcessAudio();
            }

            if (ws != null)
            {
                if (ws.State == WebSocketState.Open)
                {
                    ws.DispatchMessageQueue();
                }
            }

            if (test)
            {
                if (test)
                {
                    Speak(testString);
                    test = false;
                }

                byte[] audioBytes = null;
                lock (ttsLock)
                {
                    if (ttsAudioBytes != null)
                    {
                        if (ttsAudioBytes != null)
                        {
                            audioBytes = ttsAudioBytes;
                            ttsAudioBytes = null;
                        }
                    }
                    if (audioBytes != null)
                    {
                        if (audioBytes != null)
                        {
                            string filePath = Path.Combine(Application.persistentDataPath, "ttsAudio.mp3");
                            try
                            {
                                File.WriteAllBytes(filePath, audioBytes);
                                Debug.Log("Audio file saved as ttsAudio.mp3");
                                Debug.Log("Persistent Data Path: " + Application.persistentDataPath);
                                StartCoroutine(PlayAudio(filePath));
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError("Failed to save audio file: " + ex.Message);
                            }
                        }

                    }

    private IEnumerator PlayAudio(string filePath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Playing audio...");
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
                ttsSource.clip = audioClip;
                ttsSource.Play();
            }
            else
            {
                Debug.LogError("Failed to load audio file: " + www.error);
            }
        }
    }


}


// ---------MP3--------
// curl \
//   -X POST \
//   -H "Authorization: Token 5b0227b54f76d599ca4feb05640d73b5a31f8aa7" \
//   -H "Content-Type: application/json" \
//   --data '
//   {
//     "text":"Hello, how can I help you today?"
//   }
//   ' \
//   "https://api.deepgram.com/v1/speak?model=aura-asteria-en" \
//   -o jsonAudio.mp3


// ---------WAV--------
// curl \
//   -X POST \
//   -H "Accept: audio/wav" \
//   -H "Authorization: Token 5b0227b54f76d599ca4feb05640d73b5a31f8aa7" \
//   -H "Content-Type: application/json" \
//   --data '
//   {
//     "text":"Hello, how can I help you today?"
//   }
//   ' \
//   "https://api.deepgram.com/v1/speak?model=aura-asteria-en" \
//   -o jsonAudio.wav


// ---------RUNNING PYTHON SERVER--------
// Run Python Server --> /usr/bin/python3 "/Users/eshaansharma/Desktop/SCCN Research/DeepGram/Metro/Assets/_Metro/Scoring/TestServer.py"
