using Mirror;
using UnityEngine;
using DG.Tweening;
using Plate;
public static class TweenReaderWriter {
    // -------- CustomTweenParams --------
    public static void WriteCustomTweenParams(this NetworkWriter writer, CustomTweenParams value) {
        writer.Write((int)value.ease);
        writer.Write(value.loops);
        writer.Write((int)value.loopType);
        writer.WriteVector3(value.strength);
    }

    public static CustomTweenParams ReadCustomTweenParams(this NetworkReader reader) {
        return new CustomTweenParams
        {
            ease = reader.Read<Ease>(),
            loops = reader.Read<int>(),
            loopType = reader.Read<LoopType>(),
            strength = reader.ReadVector3()
        };
    }

    // -------- TweenInstance --------
    public static void WriteTweenInstance(this NetworkWriter writer, TweenInstance value) {
        writer.WriteString(value.name);
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
            activeInstances = new SyncList<TweenInstance>()
        };

        int count = reader.ReadInt();
        for (int i = 0; i < count; i++) {
            enumerator.activeInstances.Add(reader.ReadTweenInstance());
        }

        return enumerator;
    }
}
