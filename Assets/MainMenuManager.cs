using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public Button startButton;
    public Button controlsButton;
    public GameObject controlsMenu;
    public bool controlMenuDisplayed = false;
    public string levelName;
    public string nextLevel = "Level1Both";
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
       
        
    }

   public void loadNextLevel(int level)
    {
        SceneManager.LoadScene(level);
    }

    public void loadControlsMenu()
    {
        if(controlMenuDisplayed)
        {
            controlsMenu.SetActive(false);
            controlMenuDisplayed = false;
        } 
        else
        {
            controlsMenu.SetActive(true);
            controlMenuDisplayed = true;  
        }
    }

}
