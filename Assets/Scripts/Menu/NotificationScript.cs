using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
public enum NotificationButton: ushort {
    None = 0,
    Ok,
    Cancel,
    Yes,
    No
}
public struct NotificationData {
    public string Title;
    public string Message;
    public NotificationButton[] Buttons;
    public Action<NotificationButton> Callback; // Callback for button press
    public NotificationData(string title, string message, NotificationButton[] buttons, Action<NotificationButton> callback = null) {
        Title = title;
        Message = message;
        Buttons = buttons;
        Callback = callback;
    }
    public override bool Equals(object obj)
    {
        if (!(obj is NotificationData))
            return false;

        var other = (NotificationData)obj;

        // Compare strings
        bool titleEquals = Title == other.Title;
        bool messageEquals = Message == other.Message;

        // Compare arrays safely
        bool buttonsEquals = (Buttons == null && other.Buttons == null) ||
                             (Buttons != null && other.Buttons != null && Buttons.SequenceEqual(other.Buttons));

        // Compare callbacks by reference equality (or use Delegate.Equals)
        bool callbackEquals = Callback == other.Callback;

        return titleEquals && messageEquals && buttonsEquals && callbackEquals;
    }
    public override int GetHashCode() {
        int hash = 17;

        hash = hash * 31 + (Title?.GetHashCode() ?? 0);
        hash = hash * 31 + (Message?.GetHashCode() ?? 0);

        if (Buttons != null) {
            foreach (var btn in Buttons)
                hash = hash * 31 + btn.GetHashCode();
        }

        hash = hash * 31 + (Callback?.GetHashCode() ?? 0);

        return hash;
    }
}

public class NotificationScript : MonoBehaviour {
    //public static NotificationScript Instance { get; private set; }
    public static readonly NotificationButton[] NoButtons = {};
    public static readonly NotificationButton[] CancelOnlyButtons = {NotificationButton.Cancel};
    public static readonly NotificationButton[] OkCancelButtons = { NotificationButton.Ok, NotificationButton.Cancel};
    public static readonly NotificationButton[] OkOnlyButtons = { NotificationButton.Ok};
    public static readonly NotificationButton[] YesNoButtons = {NotificationButton.Yes, NotificationButton.No};

    public static List<NotificationData> DataList { get; private set; } = new();
    public static NotificationData CurrentData { get; private set; }
    public static bool Visible { get; private set; } = true;
    private static CanvasGroup notificationGroup;
    private static TextMeshProUGUI titleText, descText;
    private static Transform buttonsContainer;
    private static LockUI notificationLock;
    private static Tween hideTween = null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        DataList.Clear();
        Visible = true;
        notificationGroup = null;
    }
    private void Awake() {
        if (notificationGroup != null) {
            Destroy(gameObject);
            return;
        }
        notificationGroup = GetComponent<CanvasGroup>();
        notificationLock = GetComponent<LockUI>();
        //notificationLock.Lock();
        StaticAwake(transform);

    }
    private static void StaticAwake(Transform transform) {
        Transform backing = transform.Find("Backing");
        buttonsContainer = backing.Find("Buttons");
        titleText = backing.Find("Title").GetComponent<TextMeshProUGUI>();
        descText = backing.Find("DescBackground").Find("DescText").GetComponent<TextMeshProUGUI>();
    }
    private void Start() {
        ToggleNotificationPanel(false, 0f, true);
#if UNITY_EDITOR
        //StartCoroutine(StartTest());
# endif
    }
#if UNITY_EDITOR
    IEnumerator StartTest() {
        for (ushort i = 0; ; i++) {
            NotificationData data = new NotificationData {
                Title = "Test Notification " + i,
                Message = "This is a test notification message.",
                Buttons = new NotificationButton[] { NotificationButton.Ok, NotificationButton.Cancel }
            };
            AddNotification(data);
            yield return new WaitForSecondsRealtime(5f);
        }
    }
#endif
    public static void ButtonPress(string buttonNameString) {
        ButtonPress((NotificationButton)Enum.Parse(typeof(NotificationButton), buttonNameString));
    }
    public static void ButtonPress(NotificationButton buttonName) {
        if (!Visible)
            return;
        //Assert.IsTrue(Visible, "Panel is not visible yet " + buttonName.ToString() + " was selected!");
        Assert.IsTrue(DataList[0].Equals(CurrentData), "Notification's CurrentData is in DataList");
        DataList.RemoveAt(0);
        Tween tween = ToggleNotificationPanel(false);
        tween.onComplete += () => {
            if (DataList.Count > 0 && !Visible) {
                Tween tween = ShowNewNotification();
                //tween.SetDelay(0.75f);
            }
        };
        if (CurrentData.Callback != null)
            CurrentData.Callback(buttonName);
        CurrentData = default;
    }
    public static void DeleteNotification(string title) {
        List<NotificationData> deletions = DataList.FindAll(d => d.Title == title);
        foreach (NotificationData deletion in deletions) {
            if (Visible && deletion.Equals(CurrentData))
                ButtonPress(NotificationButton.None);
            else
                DataList.Remove(deletion);
        }
    }
    private static Tween ToggleNotificationPanel(bool show, float duration = 0.25f, bool started = false) {
        if (show == Visible)
            return null;
        Visible = show;
        if (!started) {
            if (show) {
                LockCore.LockAll();
                notificationLock.AddExemption(LockCore.globalCount); // draw over it!
            }
            else {
                notificationLock.RemoveExemption(LockCore.globalCount); // draw over it!
                LockCore.UnlockAll();
            }
        }

        Tween tween = hideTween = GenericTweens.TweenCanvasGroup(notificationGroup, show ? 1 : 0, 0.25f, notificationLock);
        tween.onKill += () => hideTween = null;
        return tween;
    }
    private static Tween ShowNewNotification() {
        Assert.IsTrue(DataList.Count > 0, "DataList is empty, cannot show notification.");
        NotificationData data = CurrentData = DataList[0];
        titleText.text = data.Title;
        descText.text = data.Message;
        float scaleWidth = 1f / data.Buttons.Length;
        ushort index = 0;
        foreach (Transform item in buttonsContainer) {
            item.gameObject.SetActive(false);
        }
        foreach (NotificationButton buttonNameEnum in data.Buttons) {
            Transform button = buttonsContainer.Find(buttonNameEnum.ToString());
            Assert.IsFalse(button.gameObject.activeInHierarchy, "[ShowNewNotification] Duplicate Button: " + buttonNameEnum);
            button.gameObject.SetActive(true);
            button.GetComponent<RectTransform>().anchorMin = new Vector2(index * scaleWidth, 0);
            button.GetComponent<RectTransform>().anchorMax = new Vector2((index + 1) * scaleWidth, 1);
            index++;
        }
        if (!Visible)
            return ToggleNotificationPanel(true);
        else
            return null;
    }
    public static int AddNotification(NotificationData data) {
        Assert.IsNotNull(data.Title, "Title cannot be null.");
        Assert.IsNotNull(data.Message, "Message cannot be null.");
        Assert.IsNotNull(data.Buttons, "Buttons cannot be null.");
        bool duplicate = 
            DataList.Any(d => d.Title == data.Title && d.Message == data.Message && d.Buttons.SequenceEqual(data.Buttons));
        if (duplicate)
            return -1;
        DataList.Add(data);
        if (!Visible && hideTween == null)
            ShowNewNotification();
        return 0;
    }
    
}
