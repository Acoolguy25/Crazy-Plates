using Mirror;
using UnityEngine;
using DG.Tweening;
using Plate;
public static class DGTweenEaseSerializer {
    public static void WriteEase(this NetworkWriter writer, Ease value) {
        writer.WriteInt((int) value);
    }
    public static void WriteLoopType(this NetworkWriter writer, LoopType value) {
        writer.WriteInt((int)value);
    }
    public static Ease ReadEase(this NetworkReader reader) {
        return (Ease)reader.ReadInt();
    }
    public static LoopType ReadLoopType(this NetworkReader reader) {
        return (LoopType)reader.ReadInt();
    }
}
public static class CustomTweenParamsSerializer {
    // -------- CustomTweenParams --------
    public static void WriteCustomTweenParams(this NetworkWriter writer, CustomTweenParams value) {
        writer.WriteEase(value.ease);
        writer.WriteInt(value.loops);
        writer.WriteLoopType(value.loopType);
        writer.WriteVector3(value.strength);
    }

    public static CustomTweenParams ReadCustomTweenParams(this NetworkReader reader) {
        return new CustomTweenParams
        {
            ease = reader.ReadEase(),
            loops = reader.ReadInt(),
            loopType = reader.ReadLoopType(),
            strength = reader.ReadVector3()
        };
    }
}
public static class TweenInstanceSerializer {
    // -------- TweenInstance --------
    public static void WriteTweenInstance(this NetworkWriter writer, TweenInstance value) {
        writer.WriteString(value.name);
        writer.WriteString(value.enumerator);
        writer.WriteVector3(value.value);
        writer.WriteCustomTweenParams(value.tweenParams);
        writer.WriteBool(value.isRelative);
        writer.WriteVector3(value.goal);
        writer.WriteDouble(value.startTime);
        writer.WriteFloat(value.duration);
    }

    public static TweenInstance ReadTweenInstance(this NetworkReader reader) {
        return new TweenInstance
        {
            name = reader.ReadString(),
            enumerator = reader.ReadString(),
            value = reader.ReadVector3(),
            tweenParams = reader.ReadCustomTweenParams(),
            isRelative = reader.ReadBool(),
            goal = reader.ReadVector3(),
            startTime = reader.ReadDouble(),
            duration = reader.ReadFloat(),
            tween = null,         // can't serialize
            onFinished = null     // not serializable
        };
    }
}
public static class TweenEnumeratorSerializer {

    // -------- TweenEnumerator --------
    public static void WriteTweenEnumerator(this NetworkWriter writer, TweenEnumerator value) {
        writer.WriteString(value.name);
        writer.WriteVector3(value.absoluteValue);
        writer.WriteVector3(value.tempOffset);
        writer.WriteVector3(value.permOffset);
        writer.WriteVector3(value.prevValue);

        // write activeInstances
        writer.WriteInt(value.activeInstances.Count);
        foreach (var inst in value.activeInstances)
            writer.WriteTweenInstance(inst);
    }

    public static TweenEnumerator ReadTweenEnumerator(this NetworkReader reader) {
        var enumerator = new TweenEnumerator
        {
            name = reader.ReadString(),
            absoluteValue = reader.ReadVector3(),
            tempOffset = reader.ReadVector3(),
            permOffset = reader.ReadVector3(),
            prevValue = reader.ReadVector3(),
            activeInstances = new()
        };

        int count = reader.ReadInt();
        for (int i = 0; i < count; i++) {
            enumerator.activeInstances.Add(reader.ReadTweenInstance());
        }

        return enumerator;
    }
}
