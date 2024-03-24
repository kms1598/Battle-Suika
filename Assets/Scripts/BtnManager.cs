using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BtnManager : MonoBehaviour
{
    public void Exit()
    {
        Application.Quit();
    }

    public void ManageUI(GameObject targetPanel)
    {
        targetPanel.SetActive(!targetPanel.activeSelf);
    }
}
