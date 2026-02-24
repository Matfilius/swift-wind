using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Main Menu Object")]
    [SerializeField] private GameObject _loadingBarObject;
    [SerializeField] private Image _loadingBar;
    [SerializeField] private GameObject[] _objcetsToHide;

    [Header("Scenes to Load")]
    [SerializeField] private string _GameplayScene="GameplayScene";

    private List<AsyncOperation> _scenesToLoad = new List<AsyncOperation>();

    private void Awake()
    {
        _loadingBarObject.SetActive(false);
    }

    public void StartGame()
    {
        HideMenu();

        _loadingBarObject.SetActive(true);

        _scenesToLoad.Add(SceneManager.LoadSceneAsync(_GameplayScene));

        StartCoroutine(ProgressLoadingBar());
    }

    private void HideMenu()
    {
        for(int i=0;i<_objcetsToHide.Length;i++)
        {
            _objcetsToHide[i].SetActive(false);
        }
    }

    private IEnumerator ProgressLoadingBar()
    {
        float loadProgress = 0f;
        for(int i=0;i< _scenesToLoad.Count;i++)
        {
            while (!_scenesToLoad[i].isDone)
            {
                loadProgress += _scenesToLoad[i].progress;
                _loadingBar.fillAmount = loadProgress / _scenesToLoad.Count;
                yield return null;
            }
        }
    }

}
