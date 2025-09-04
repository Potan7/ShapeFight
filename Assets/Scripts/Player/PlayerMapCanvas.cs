using TMPro;
using UnityEngine;

public class PlayerMapCanvas : MonoBehaviour
{
    public TextMeshProUGUI ammoText;

    public void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
    }
    public void ReloadAmmo()
    {
        ammoText.text = "Ammo: Reloading...";
    }
}
