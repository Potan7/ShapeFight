using UnityEngine;
using UnityEngine.UI;

public class PlayerWorldCanvas : MonoBehaviour
{
    public Image reloadImage;

    public void UpdateReloadUI(float reloadProgress)
    {
        reloadImage.fillAmount = reloadProgress;
        reloadImage.gameObject.SetActive(reloadProgress < 1f);
    }
}
