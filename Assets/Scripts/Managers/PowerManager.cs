﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerManager : MonoBehaviour
{
    private Dictionary<IPowerConduit, HashSet<IPowerConduit>> connectionMap
        = new Dictionary<IPowerConduit, HashSet<IPowerConduit>>();
    private List<IPowerConduit> powerConduits = new List<IPowerConduit>();

    private void Start()
    {
        Managers.Scene.onSceneLoaded += (s) => generateConnectionMap();
        Managers.Scene.onSceneUnloaded += (s) => generateConnectionMap();
        generateConnectionMap();
    }

    private void FixedUpdate()
    {
        //Reset all power wires
        powerConduits
              .FindAll(ipc => ipc is PowerWire)
              .ConvertAll(ipc => (PowerWire)ipc)
              .ForEach(pw => pw.reset());
        //Have powerers dish out their power
        List<IPowerer> powerers = powerConduits
           .FindAll(ipc => ipc is IPowerer)
           .ConvertAll(ipc => (IPowerer)ipc);
        List<IPowerable> noPowerPowerables = powerConduits
           .FindAll(ipc => ipc is IPowerable)
           .ConvertAll(ipc => (IPowerable)ipc);
        powerers.ForEach(ipr =>
        {
            List<IPowerable> powerables = getPowerables(ipr);
            float powerToEach = ipr.ThroughPut * Time.fixedDeltaTime / powerables.Count;
            powerables.ForEach(pwr => ipr.givePower(
                -pwr.acceptPower(ipr.givePower(powerToEach))
                ));
            powerables.ForEach(pwr => noPowerPowerables.Remove(pwr));
        }
            );
        //Process powerables with no power
        noPowerPowerables.ForEach(pwr => pwr.acceptPower(0));
    }

    private List<IPowerable> getPowerables(IPowerer source)
    {
        HashSet<IPowerable> powerables = new HashSet<IPowerable>();
        List<IPowerTransferer> wires = connectionMap[source].ToList()
            .FindAll(ipc => ipc is IPowerTransferer)
            .ConvertAll(ipc => (IPowerTransferer)ipc);
        int i = 0;
        while (i < wires.Count)
        {
            connectionMap[wires[i]].ToList().ForEach(
                ipc =>
                {
                    if (ipc is IPowerable && ipc.GameObject != source.GameObject)
                    {
                        powerables.Add((IPowerable)ipc);
                    }
                    if (ipc is IPowerTransferer)
                    {
                        if (!wires.Contains(ipc))
                        {
                            wires.Add((IPowerTransferer)ipc);
                        }
                    }
                }
                );
            i++;
        }
        return powerables.ToList();
    }

    private void generatePowerConduitList()
    {
        powerConduits.Clear();
        powerConduits.AddRange(
            FindObjectsOfType<GameObject>().ToList()
           .FindAll(go => go.GetComponent<IPowerConduit>() != null)
           .ConvertAll(go => go.GetComponent<IPowerConduit>())
        );
    }

    public void generateConnectionMap()
    {
        generatePowerConduitList();
        connectionMap.Clear();
        List<IPowerer> powerers = powerConduits
           .FindAll(ipc => ipc is IPowerer)
           .ConvertAll(ipc => (IPowerer)ipc);
        powerers.ForEach(ipr =>
        {
            generateConnections(ipr);
        });
    }
    private void generateConnections(IPowerConduit ipc)
    {
        //Don't process a conduit twice
        if (connectionMap.ContainsKey(ipc))
        {
            return;
        }
        else
        {
            connectionMap.Add(ipc, new HashSet<IPowerConduit>());
        }
        //Get list of connecting conduits
        List<IPowerConduit> connectingConduits = getConnectingConduits(ipc);
        //Connect this conduit with a connecting one
        connectingConduits.ForEach(cc => addConnection(ipc, cc));
        //Connect further
        connectingConduits.ForEach(cc => generateConnections(cc));
    }
    private void addConnection(IPowerConduit node, IPowerConduit neighbor)
    {
        if (!connectionMap.ContainsKey(node))
        {
            connectionMap.Add(node, new HashSet<IPowerConduit>());
        }
        connectionMap[node].Add(neighbor);
    }



    public List<IPowerConduit> getConnectingConduits(IPowerConduit ipc)
    {
        Collider2D coll2d = ipc.GameObject.GetComponent<Collider2D>();
        Collider2D[] colls = new Collider2D[Utility.MAX_HIT_COUNT];
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        int count = coll2d.OverlapCollider(filter, colls);
        Utility.checkMaxReturnedList("getConnectingConduits", count);
        List<IPowerConduit> conduits = new List<IPowerConduit>();
        for (int i = 0; i < count; i++)
        {
            IPowerConduit conduit = colls[i].gameObject.GetComponent<IPowerConduit>();
            if (conduit != null)
            {
                conduits.Add(conduit);
            }
        }
        return conduits;
    }
}