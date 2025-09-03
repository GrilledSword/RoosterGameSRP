using System.Collections;
using UnityEngine;

public class LoadingPopUpManager : MonoBehaviour
{
    [SerializeField] private GameObject loadingPopUp;
    [SerializeField] private float displayDuration = 2f;
    private void Start()
    {
        if (loadingPopUp != null)
        {
            loadingPopUp.SetActive(false);
        }
    }
    public void ShowLoadingPopUp()
    {
        if (loadingPopUp != null)
        {
            loadingPopUp.SetActive(true);
            StartCoroutine(Countdown());
        }
    }
    private void HideLoadingPopUp()
    {
        if (loadingPopUp != null)
        {
            loadingPopUp.SetActive(false);
        }
    }

    private IEnumerator Countdown()
    {
        yield return new WaitForSeconds(displayDuration);
        HideLoadingPopUp();
    }
}
