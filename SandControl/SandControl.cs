using OWML.Common;
using OWML.ModHelper;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SandControl;

public class SandControl : ModBehaviour
{
    public static SandControl Instance;

    private SandLevelController _towerTwinSand, _caveTwinSand;
    private AnimationCurve _towerTwinInverseCurve;

    private float _sandPercentage;

    private SandFunnelController _sandFunnelController;

    private bool _sandFunnelActive;

    public void Start()
    {
        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
    }

    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
    {
        if (newScene != OWScene.SolarSystem) return;

        var towerTwinOriginalCurve = Locator.GetAstroObject(AstroObject.Name.TowerTwin).GetComponentInChildren<SandLevelController>(true)._scaleCurve;
        _towerTwinInverseCurve = new AnimationCurve();
        for (int i = 1; i < towerTwinOriginalCurve.length - 1; i++)
        {
            _towerTwinInverseCurve.AddKey(towerTwinOriginalCurve.keys[i].value, towerTwinOriginalCurve.keys[i].time);
        }

        _towerTwinSand = Locator.GetAstroObject(AstroObject.Name.TowerTwin).GetComponentInChildren<SandLevelController>(true);
        _caveTwinSand = Locator.GetAstroObject(AstroObject.Name.CaveTwin).GetComponentInChildren<SandLevelController>(true);

        _sandFunnelController = GameObject.Find("SandFunnel_Body").GetComponent<SandFunnelController>();

        TryUpdateSandLevels();

        // Disable base game sand controls
        _sandFunnelController._growAfterMinutes = float.MaxValue;
        _sandFunnelController._shrinkAfterMinutes = float.MaxValue;

        SetSandFunnelActive(_sandFunnelActive);
    }

    public void Update()
    {
        if (LoadManager.GetCurrentScene() == OWScene.SolarSystem)
        {
            if (Keyboard.current[Key.NumpadPlus].isPressed)
            {
                _sandPercentage += 0.05f * Time.deltaTime;
                TryUpdateSandLevels();
            }
            if (Keyboard.current[Key.NumpadMinus].isPressed)
            {
                _sandPercentage -= 0.05f * Time.deltaTime;
                TryUpdateSandLevels();
            }

            // Only update config value once they release the key
            if (Keyboard.current[Key.NumpadMinus].wasReleasedThisFrame || Keyboard.current[Key.NumpadPlus].wasReleasedThisFrame)
            {
                ModHelper.Config.SetSettingsValue("sandPercentage", _sandPercentage * 100f);
            }

            if (Keyboard.current[Key.Digit0].wasPressedThisFrame)
            {
                SetSandFunnelActive(!_sandFunnelActive);
                ModHelper.Config.SetSettingsValue("sandFunnelActive", _sandFunnelActive);
            }
        }
    }

    public override void Configure(IModConfig config)
    {
        base.Configure(config);
        _sandPercentage = Mathf.Clamp01(config.GetSettingsValue<float>("sandPercentage") / 100f);

        _sandFunnelActive = config.GetSettingsValue<bool>("sandFunnelActive");

        TryUpdateSandLevels();
        SetSandFunnelActive(_sandFunnelActive);
    }

    private void TryUpdateSandLevels()
    {
        if (LoadManager.GetCurrentScene() == OWScene.SolarSystem)
        {
            try
            {
                // 330 -> 76
                var ttSand = Mathf.Lerp(330f, 76f, _sandPercentage);

                // 30 -> 310
                var ctSand = Mathf.Lerp(30f, 310f, _sandPercentage);

                _towerTwinSand._scaleCurve = new AnimationCurve(new Keyframe(0, ttSand));
                _caveTwinSand._scaleCurve = new AnimationCurve(new Keyframe(0, ctSand));

                var rawTime = _towerTwinInverseCurve.Evaluate(ttSand);
                var minutes = Mathf.FloorToInt(rawTime);
                var seconds = Mathf.FloorToInt(rawTime * 60f) - (minutes * 60);
                var timeString = minutes.ToString() + ":" + seconds.ToString("00");

                ModHelper.Console.WriteLine($"At {_sandPercentage * 100f}% the Ash Twin sand is at {ttSand}m and the Ember Twin sand is at {ctSand}m. This level would occur at {timeString} into the loop.", MessageType.Info);
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine($"Couldn't change sand level: {e}", MessageType.Error);
            }
        }
    }

    private void SetSandFunnelActive(bool active)
    {
        _sandFunnelActive = active;

        if (LoadManager.GetCurrentScene() == OWScene.SolarSystem)
        {
            try
            {
                if (active)
                {
                    _sandFunnelController._hasStartedGrowing = true;
                    _sandFunnelController.CheckVolumeAndEffectActivation(false);
                    _sandFunnelController.ScaleTo(1f, 10f);
                    _sandFunnelController.SetGeoActive(true);
                }
                else
                {
                    _sandFunnelController._hasStartedShrinking = true;
                    _sandFunnelController.CheckVolumeAndEffectActivation(false);
                    _sandFunnelController.ScaleTo(0.001f, 10f);
                }
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine($"Couldn't toggle sand funnel: {e}", MessageType.Error);
            }
        }

    }
}