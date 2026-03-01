using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Editor tool: tạo Animator Controller cho Enemy với trigger Hit.
/// Vào menu: Tools > Create Enemy Hit Controller
/// </summary>
public class CreateEnemyHitController
{
    [MenuItem("Tools/Create Enemy Hit Controller")]
    public static void Create()
    {
        // Tìm clip Enemy_Hit
        string[] guids = AssetDatabase.FindAssets("Enemy_Hit t:AnimationClip");
        if (guids.Length == 0)
        {
            Debug.LogError("[CreateEnemyHitController] Không tìm thấy clip Enemy_Hit!");
            return;
        }

        string clipPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        AnimationClip hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        Debug.Log($"[CreateEnemyHitController] Dùng clip: {clipPath}");

        // Tạo controller mới
        string savePath = "Assets/Project/Animations/EnemyHit.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);

        // Thêm trigger parameter "Hit"
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

        // Lấy root state machine
        AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

        // Thêm state EnemyHit
        AnimatorState hitState = rootSM.AddState("EnemyHit");
        hitState.motion = hitClip;

        // Transition: Any State -> EnemyHit (trigger Hit)
        AnimatorStateTransition anyToHit = rootSM.AddAnyStateTransition(hitState);
        anyToHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        anyToHit.hasExitTime = false;
        anyToHit.duration = 0.05f;
        anyToHit.canTransitionToSelf = false;

        // Transition: EnemyHit -> Exit
        AnimatorStateTransition hitToExit = hitState.AddExitTransition();
        hitToExit.hasExitTime = true;
        hitToExit.exitTime = 0.9f;
        hitToExit.duration = 0.05f;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CreateEnemyHitController] Tạo thành công: {savePath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = controller;
    }
}
