using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using VRC.SDKBase;

namespace VRCTranscriptMod.VRCTranscribe {
    class SubtitleUi {
        // TODO: Indicate confidence level?

        static GameObject subtitleParent;
        public static void Init() {
            MelonLogger.Msg("Init SubtitleUi");
            subtitleParent = new GameObject();
            subtitleParent.name = "SubtitleUI";
            UnityEngine.Object.DontDestroyOnLoad(subtitleParent);
            subtitleParent.transform.SetParent(GameObject.Find("UserInterface").transform);
        }

        TranscriptSession session;

        public SubtitleUi(TranscriptSession session) {
            this.session = session;
            InitText();
        }

        TextMeshPro textMesh;
        TextMeshPro textMeshBg;
        GameObject textObj;

        Transform tgt_transform;
        Vector3 remoteHeadPositionSmooth = Vector3.zero;
        Vector3 forwardDirectionSmooth = Vector3.zero;


        void CalculateSubtitleTransform() {
            if(tgt_transform == null) MelonLogger.Msg("CalculateSubtitleTrasform NULL TGT!!");
            Vector3 localHeadPosition = Networking.LocalPlayer.GetBonePosition(HumanBodyBones.Head);

            if(
                // when the position isn't initialized
                remoteHeadPositionSmooth.sqrMagnitude < 0.001f ||

                // OR when the player has been teleported (over 16 units)
                (remoteHeadPositionSmooth - tgt_transform.position).sqrMagnitude > (16.0f*16.0f)
            ) {
                // don't smooth it
                remoteHeadPositionSmooth = tgt_transform.position;
            } else {
                remoteHeadPositionSmooth = Vector3.Lerp(remoteHeadPositionSmooth, tgt_transform.position, Time.deltaTime);
            }

            Vector3 forwardDirection = remoteHeadPositionSmooth - localHeadPosition;
            if(forwardDirectionSmooth.sqrMagnitude < 0.001f) {
                forwardDirectionSmooth = forwardDirection;
            } else {
                forwardDirectionSmooth = Vector3.Lerp(forwardDirectionSmooth, forwardDirection, Time.deltaTime * 3.0f);
                forwardDirectionSmooth.Normalize();
            }

            float distance = forwardDirection.sqrMagnitude;

            float dist_fw = -(1.0f / (distance + 1.0f)) + 0.75f;

            Quaternion quat = Quaternion.LookRotation(forwardDirectionSmooth);
            if(quat == null) MelonLogger.Msg("QUAT IS NULL? WTF?");

            Vector3 offset = (quat * Vector3.right) * 0.0f;// + (quat * Vector3.forward) * 0.25f + (quat * Vector3.down) * 0.25f;

            Vector3 frontVectorFlatPlane = (quat * Vector3.forward);
            frontVectorFlatPlane.Scale(new Vector3(1.0f, 0.0f, 1.0f));
            frontVectorFlatPlane.Normalize();

            offset = offset - frontVectorFlatPlane * dist_fw;
            offset = offset + (Vector3.down * 0.25f);

            Vector3 finalPosition = remoteHeadPositionSmooth + offset;
            Quaternion finalQuat = Quaternion.LookRotation(finalPosition - localHeadPosition);

            textObj.transform.rotation = finalQuat;
            textObj.transform.position = finalPosition;
            textObj.transform.localScale = new Vector3(0.033f, 0.033f, 0.033f);
        }

        public void InitText() {
            tgt_transform = session.associated_player.gameObject.transform.Find("AnimationController/HeadAndHandIK/HeadEffector");

            if(tgt_transform == null) MelonLogger.Error("TGT TRANSFORM IS NULL!!!");

            textObj = new GameObject();
            textObj.name = Utils.GetUID(session.associated_player);
            textMesh = textObj.AddComponent<TextMeshPro>();

            textObj.transform.SetParent(subtitleParent.transform);

            textObj.layer = 10;
            
            // TODO; one ui occluding another

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(42, 1);

            textMesh.fontSize /= 2.0f;   
            textMesh.autoSizeTextContainer = false;
            textMesh.richText = true;
            textMesh.enableWordWrapping = false;

            textMesh.fontMaterial.shader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");

            GameObject clone = UnityEngine.Object.Instantiate(textObj);
            clone.transform.SetParent(textObj.transform);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;

            textMeshBg = clone.GetComponent<TextMeshPro>();

            textMeshBg.sortingOrder = 2;
            textMesh.sortingOrder = 3;
        }

        public void UpdateText() {
            if(textObj == null) return;
            textMesh.text = session.GetText();
            textMeshBg.text = "<mark=#000000aa padding=\"10,10,10,10\">" + session.GetText();
            CalculateSubtitleTransform();
        }

        public void Dispose() {
            if(textObj != null) {
                textObj.SetActive(false);
                UnityEngine.Object.Destroy(textObj);
                textObj = null;
            }
        }
    }
}
