using TMPro;
using UnityEngine;

public class HUD : MonoBehaviour
{
    [SerializeField] private Boat mBoat;

    [SerializeField] private GameObject mWindIndicator;
    [SerializeField] private TextMeshProUGUI mWindSpeedText;

    private WindManager mWindManager;


    private void Start()
    {
        mWindManager = FindAnyObjectByType<WindManager>();
    }

    private void Update()
    {
        float windDirection = Vector3.Angle(mBoat.GetLinearVelocity(), mWindManager.GetWind());
        mWindIndicator.transform.localRotation = Quaternion.Euler(0, 0, windDirection);

        float windSpeed = Mathf.FloorToInt(mWindManager.GetWindStrength() * 4) / 4;
        mWindSpeedText.text = windSpeed.ToString() + " knots";
    }
}
