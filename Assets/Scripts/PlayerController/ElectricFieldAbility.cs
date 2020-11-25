﻿using UnityEngine;

public class ElectricFieldAbility : PlayerAbility
{//2017-11-17: copied from ShieldBubbleAbility

    [Header("Settings")]
    public float maxRange = 2.5f;
    public float maxEnergy = 100;//not the maximum for the player's electric fields
    public float maxChargeTime = 1;//how long until the max range is reached after it begins charging
    public float maxSlowPercent = 0.10f;//the percent of slowness applied to objects in the field when the field has maxRange
    public float maxForceResistance = 500f;//how much force is required to deal 100% damage to the field at max range
    
    [Header("Components")]
    public GameObject electricFieldPrefab;//prefab
    public AudioClip shieldBubbleSound;

    private ElectricFieldController cEFController;//"current Electric Field Controller"

    private bool activated = false;//true if Merky is currently using this ability

    
    protected override void init()
    {
        base.init();
        if (playerController)
        {
            playerController.onPreTeleport += processTeleport;
        }
    }
    public override void OnDisable()
    {
        base.OnDisable();
        if (playerController)
        {
            playerController.onPreTeleport -= processTeleport;
        }
    }

    void Update()
    {
        if (!Managers.Game.Rewinding)
        {
            if (activated)
            {
                //Make a field
                chargeField();
            }
            else
            {
                //Unmake the field
                dischargeField();
            }
        }
    }

    public void chargeField()
    {
        //If he's not charging one yet,
        if (cEFController == null)
        {
            //Create a new one
            GameObject currentElectricField = Utility.Instantiate(electricFieldPrefab);
            cEFController = currentElectricField.GetComponent<ElectricFieldController>();
            cEFController.energyToRangeRatio = maxRange / maxEnergy;
            cEFController.energyToSlowRatio = maxSlowPercent / maxEnergy;
            cEFController.maxForceResistance = maxForceResistance;
            //Update Stats
            GameStatistics.addOne("ElectricFieldField");
        }
        //Make the field follow him
        cEFController.transform.position = transform.position;
        //Charge the field
        float energyToAdd = Time.deltaTime * maxEnergy / maxChargeTime;
        cEFController.addEnergy(energyToAdd, maxEnergy);
    }

    public void dischargeField()
    {
        //If he's charging one,
        if (cEFController != null)
        {
            //Make the field follow him
            cEFController.transform.position = transform.position;
            //Discharge the field
            float energyToSubtract = -1 * Time.deltaTime * maxEnergy / maxChargeTime;
            cEFController.addEnergy(energyToSubtract);
            //If the field has no energy,
            if (cEFController.energy <= 0)
            {
                //Drop it
                dropField();
            }
        }
    }

    public void dropField()
    {
        cEFController = null;
    }

    public void processTeleport(Vector2 oldPos, Vector2 newPos, Vector2 triedPos)
    {
        //If player tapped on Merky,
        if (playerController.gestureOnPlayer(triedPos))
        {
            //If not activated,
            if (!activated)
            {
                //Activate
                activated = true;
                //Update Stats
                GameStatistics.addOne("ElectricField");
            }
            //Else,
            else
            {
                //Deactivate or detach
                activated = false;
            }
        }
    }
}
