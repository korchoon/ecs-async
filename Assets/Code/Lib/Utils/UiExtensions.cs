using Mk.Routines;
using Mk.Scopes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

static class UiExtensions {
    public static void AddListener (this UnityEvent e, ISafeScope scope, UnityAction callback) {
        e.AddListener (callback);
        scope.Add (() => e.RemoveListener (callback));
    }

    public static void SetActive (this GameObject e, ISafeScope scope, bool value) {
        if (e.activeSelf == value) {
            return;
        }

        e.SetActive (value);
        scope.Add (() => e.SetActive (!value));
    }

    public static async Routine WaitForClick (this Button button) {
        var clicked = false;
        var scope = await Routine.GetScope ();
        button.onClick.AddListener (scope, () => clicked = true);

        await Routine.When (() => clicked);
    }
}