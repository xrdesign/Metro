using UnityEngine;
using UnityEngine.UI;
public class PngSequencePlayer : MonoBehaviour
{
    public string folderPath = "MicGif";
    public float frameRate = 30f;

    Texture2D[] frames;
    int currentFrame = 0;
    float timer = 0f;
    Image img;

    void Start()
    {
        img = GetComponent<Image>();
        frames = Resources.LoadAll<Texture2D>(folderPath);
        if (frames == null || frames.Length == 0)
            Debug.LogError("No frames found in Resources/" + folderPath);
        else
            img.sprite = Sprite.Create(frames[0], new Rect(0, 0, frames[0].width, frames[0].height), new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            currentFrame = (currentFrame + 1) % frames.Length;
            img.sprite = Sprite.Create(frames[currentFrame], new Rect(0, 0, frames[currentFrame].width, frames[currentFrame].height), new Vector2(0.5f, 0.5f));
            timer -= 1f / frameRate;
        }
    }
}