using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Felbourn
{    
    public class ModuleFairingDecoupler : ModuleAnchoredDecoupler
    {
        [KSPField]
        public string decouplerNode = "";
        [KSPField]
        public string payloadNode = "";
        [KSPField(guiActive = true)]
        private bool shielding = true;

        private List<Part> shielded = new List<Part>();

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);

            // I'm a nose fairing on a decoupler. Find my decoupler.
            AttachNode decouplerAttach = part.findAttachNode(decouplerNode);
            if (decouplerAttach == null)
            {
                Debug.LogError("ModuleFairingDecoupler - error - can't find decoupler node: " + decouplerNode);
                return;
            }
            Debug.Log("ModuleFairingDecoupler - info - using attachment from " + part.name + "." + decouplerNode);

            Part decoupler = decouplerAttach.attachedPart;
            if (decoupler == null)
            {
                Debug.LogError("ModuleFairingDecoupler - not attached to decoupler, not shielding anything");
                shielding = false; // not attached to decoupler
                return;
            }
            shielded.Add(decoupler); // add decoupler so we don't recurse through it
            
            // I know the decoupler, so find its payload.
            Debug.Log("ModuleFairingDecoupler - info - searching up from " + decoupler.name + "." + payloadNode);
            AttachNode payloadAttach = decoupler.findAttachNode(payloadNode);
            if (payloadAttach == null)
            {
                Debug.LogError("ModuleFairingDecoupler - error - can't find payload node: " + payloadNode);
                return;
            }
            Part payload = payloadAttach.attachedPart;
            if (payload == null)
            {
                Debug.LogError("ModuleFairingDecoupler - no payload, not shielding anything");
                shielding = false; // no payload in decoupler
                return;
            }

            // recursively add first payload part and all children
            Debug.Log("ModuleFairingDecoupler - info - shielding payload via " + payload.name);
            ShieldPart(payload);

            // now add all surface attached parts that connect to a shielded part
            for (int i = 0; i < 100; i++)
                if (!AddRadialParts())
                    break;
        }

        private bool AddRadialParts()
        {
            Debug.Log("ModuleFairingDecoupler - info - AddRadialParts iteration");
            if (vessel == null)
                return false;
            if (vessel.parts == null)
                return false;

            bool again = false;
            for (int i = vessel.parts.Count - 1; i >= 0; i--)
            {
                Part radial = vessel.parts[i];
                if (radial == null)
                    continue; // should not happen
                if (radial.srfAttachNode == null)
                    continue; // can this part surface attach to something?
                Part parent = radial.srfAttachNode.attachedPart;
                if (parent == null)
                    continue; // is it surface attached to something?
                if (shielded.Contains(radial))
                    continue; // did we already add ourself to the list?
                if (!shielded.Contains(parent))
                    continue; // is the thing we're attached to shielded?

                Debug.Log("ModuleFairingDecoupler - info - shield radial from: " + parent.partInfo.name + " into: " + radial.partInfo.name);
                ShieldPart(radial);
                again = true;
            }
            return again;
        }

        private void ShieldPart(Part parent)
        {
            parent.ShieldedFromAirstream = true;
            shielded.Add(parent);

            foreach (AttachNode childAttach in parent.attachNodes)
            {
                Part child = childAttach.attachedPart;
                if (child == null)
                {
                    //Debug.Log("ModuleFairingDecoupler - info - no attach from: " + parent.partInfo.name + " at: " + childAttach.id);
                    continue;
                }
                if (shielded.Contains(child))
                {
                    //Debug.Log("ModuleFairingDecoupler - info - seen: " + parent.partInfo.name + " from: " + child.partInfo.name);
                    continue;
                }
                Debug.Log("ModuleFairingDecoupler - info - shield: " + child.partInfo.name + " via: " + parent.partInfo.name);
                ShieldPart(child);
            }
        }

        public void FixedUpdate()
        {
            if (!shielding)
                return;
            if (!isDecoupled)
                return;

            Debug.Log("ModuleFairingDecoupler - info - payload exposed");
            shielding = false;
            foreach (Part p in shielded)
                p.ShieldedFromAirstream = false;
            shielded = null;
        }
    }
}
