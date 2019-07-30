using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTimerManager : MonoBehaviour
{
    public float m_MaxLevelTimer = 420;
    public bool ForceQuit = false;

    private LSLMarkerScript m_MarkerScript;
    private GameObject m_CoinList;

    private void Start()
    {
        GameObject LSLMarkerObject = GameObject.FindGameObjectWithTag("MarkerObject");
        if (LSLMarkerObject != null)
        {
            m_MarkerScript = LSLMarkerObject.GetComponent<LSLMarkerScript>();
        }

        m_CoinList = GameObject.FindGameObjectWithTag("CoinListObject");
    }

    // Update is called once per frame
    void Update()
    {
        m_MaxLevelTimer -= Time.deltaTime;
        if (m_MaxLevelTimer <= 0 || ForceQuit)
        {
            m_MarkerScript.PushData("GAME_END");
            LoadNewScene();
        }
    }


    void LoadNewScene()
    {
        m_CoinList.SetActive(false);
        SceneManager.LoadScene(3);
    }
}
