using TMPro;
using UnityEngine;

public class GameManagerTirabolas : MonoBehaviour
{
    private int hitCount=0;
    public TextMeshProUGUI contgolpes;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void incrementarGolpes() { 
        this.hitCount++;
        this.contgolpes.text = $"Has golpeado: {hitCount} dianas";
    }
}
