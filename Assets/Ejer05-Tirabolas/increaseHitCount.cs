using UnityEngine;

public class increaseHitCount : MonoBehaviour
{
    private GameManagerTirabolas gameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //asignamos el gamemanager para que lo busqyue en la escena
        gameManager = Object.FindAnyObjectByType<GameManagerTirabolas>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("target"))
        {

            gameManager.incrementarGolpes();
        }
    }
}
