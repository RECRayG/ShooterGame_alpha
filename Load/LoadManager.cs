using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class LoadManager : MonoBehaviour
{
    public static LoadManager Instance;

    [SerializeField]
    private GameObject loaderCanvas;

    [SerializeField]
    private Image loadBar;
    [SerializeField]
    private Image loadBarShadow;
    [SerializeField]
    private TextMeshProUGUI percentOfLoad;
    [Space]
    [SerializeField]
    private Animator animatorLogo;
    [SerializeField]
    private Animator animatorFade;

    private float target;
    private AsyncOperation loadingSceneOperation;
    private int index;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(int sceneIndex)
    {
		Instance.loadBar.fillAmount = 0f;
        Instance.loadBarShadow.fillAmount = 0f;
        Instance.percentOfLoad.text = "0%";
		
        Instance.index = sceneIndex;

        Instance.loadingSceneOperation = SceneManager.LoadSceneAsync(Instance.index);
        Instance.loadingSceneOperation.allowSceneActivation = false;

		Instance.animatorFade.SetTrigger("FadeOut");
        Instance.animatorFade.ResetTrigger("FadeIn");
    }

    private void Update()
    {
        if(Instance.loadingSceneOperation != null && Instance.loadingSceneOperation.isDone)
        {
            Instance.animatorLogo.SetBool("Loading", false);
            Instance.loaderCanvas.SetActive(false);

            Instance.animatorFade.ResetTrigger("FadeOut");
            Instance.animatorFade.SetTrigger("FadeIn");

            //Instance.loadingSceneOperation.allowSceneActivation = true;
        }

        Instance.loadBar.fillAmount = Mathf.MoveTowards(Instance.loadBar.fillAmount, Instance.target, 3 * Time.deltaTime);
        Instance.loadBarShadow.fillAmount = Mathf.MoveTowards(Instance.loadBarShadow.fillAmount, Instance.target, 3 * Time.deltaTime);
        Instance.percentOfLoad.text = Mathf.RoundToInt((Instance.target) * 100) + "%";
    }

    public async void ShowScreen()
    {
        Instance.loaderCanvas.SetActive(true);
        Instance.animatorLogo.SetBool("Loading", true);

        do
        {
            await Task.Delay(100);
            Instance.target = Instance.loadingSceneOperation.progress;
        } while (Instance.loadingSceneOperation.progress < .9f);

        Instance.loadingSceneOperation.allowSceneActivation = true;
    }
}
