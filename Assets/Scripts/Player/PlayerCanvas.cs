using TMPro;
using UnityEngine;

public class PlayerCanvas : MonoBehaviour
{
    public TextMeshProUGUI ammoText;

    public void UpdateAmmo(int currentAmmo, int maxAmmo)
    {
        ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
    }
}
