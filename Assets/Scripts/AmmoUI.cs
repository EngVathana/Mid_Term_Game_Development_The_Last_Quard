using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    public Gun gun;
    public TMPro.TMP_Text ammoText;
    public TMPro.TMP_Text reloadText;

    void LateUpdate()
    {
        if (gun == null || ammoText == null || reloadText == null)
        {
            return;
        }

        ammoText.text = gun.CurrentAmmo + " / " + gun.magazineSize;
        ammoText.color = gun.CurrentAmmo == 0 ? Color.red : Color.white;

        reloadText.text = "Reloading...";
        reloadText.gameObject.SetActive(gun.IsReloading);
    }
}
