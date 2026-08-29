using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ProjectLuna.WebAnimatic
{
    /// <summary>
    /// 웹 애니매틱(Cutscene/web) 씬 데이터의 유니티 측 표현.
    /// 웹 engine.js와 같은 규칙(화면 상태 = 시간 t의 순수 함수)으로 평가한다.
    /// 좌표계는 웹 그대로(px, y는 아래로 증가)이며 렌더 시 (x/100, -y/100)로 변환한다.
    /// </summary>
    [Serializable]
    public class Key
    {
        public float t;
        public float v;
        public string ease;
    }

    [Serializable]
    public class StepKeyS { public float t; public string v; }

    [Serializable]
    public class ActorData
    {
        public string id;
        public string displayName;
        public int z;
        public string tint;
        public float tintAmount;
        public float bob;
        public float bobHz;
        public List<Key> x, y, rot, scale, alpha;
        public List<StepKeyS> anim;
        public List<Key> flip, vis;
    }

    [Serializable]
    public class FxData
    {
        public string type;
        public float t, dur;
        public float amount, atk, rel;
        public float x, y, x2, y2, scale, alpha, amp, hz, peak, falloff, sag, w;
        public float groundY, power, seed;
        public int count;
        public bool flip;
        public string color, dir, a, b, tint;
        public float ax, ay, bx, by;
    }

    [Serializable]
    public class PauseData { public int i; public float d; }

    [Serializable]
    public class SpanData { public string type; public int i0, i1; public string param; }

    [Serializable]
    public class LineData
    {
        public int idx;
        public float t, dur, lead, cps;
        public string speaker, text, raw, kind, actor;
        public float[] at;           // 월드 좌표 앵커(스피커 등), 없으면 null
        public bool below, glitch;
        public List<PauseData> pauses;
        public List<SpanData> spans;
    }

    [Serializable]
    public class CueData { public float t; public string label; }

    [Serializable]
    public class CutData { public float t; public string label; public string note; }

    [Serializable]
    public class StageSwitchData { public float t; public string id; }

    public class SceneData
    {
        public float end;
        public bool muteAlarm;
        public float alarmGain = 1f;
        public string radioActor;
        public List<StageSwitchData> stages = new();
        public List<ActorData> actors = new();
        public List<Key> camX = new(), camY = new(), camZ = new();
        public List<FxData> fx = new();
        public List<CueData> sfx = new();
        public List<CutData> cuts = new();
        public Dictionary<string, List<Key>> stageTracks = new();
        public List<LineData> lines = new();

        public static SceneData Parse(string json)
        {
            JObject o = JObject.Parse(json);
            SceneData s = new()
            {
                end = o.Value<float>("end"),
                muteAlarm = o.Value<bool?>("muteAlarm") ?? false,
                alarmGain = o.Value<float?>("alarmGain") ?? 1f,
                radioActor = o.Value<string>("radioActor"),
            };
            foreach (JObject e in o["stages"]!)
                s.stages.Add(new StageSwitchData { t = e.Value<float>("t"), id = e.Value<string>("id") });
            foreach (JObject a in o["actors"]!)
            {
                ActorData ad = new()
                {
                    id = a.Value<string>("id"),
                    displayName = a.Value<string>("name"),
                    z = a.Value<int?>("z") ?? 0,
                    tint = a.Value<string>("tint"),
                    tintAmount = a.Value<float?>("tintAmount") ?? 0.4f,
                    bob = a.Value<float?>("bob") ?? 0f,
                    bobHz = a.Value<float?>("bobHz") ?? 0f,
                    x = Keys(a["x"]), y = Keys(a["y"]), rot = Keys(a["rot"]),
                    scale = Keys(a["scale"]), alpha = Keys(a["alpha"]),
                    flip = Keys(a["flip"]), vis = Keys(a["vis"]),
                    anim = new List<StepKeyS>(),
                };
                foreach (JObject k in a["anim"]!)
                    ad.anim.Add(new StepKeyS { t = k.Value<float>("t"), v = k.Value<string>("v") });
                s.actors.Add(ad);
            }
            s.camX = Keys(o["camX"]); s.camY = Keys(o["camY"]); s.camZ = Keys(o["camZ"]);
            foreach (JObject f in o["fx"]!)
            {
                FxData fd = new()
                {
                    type = f.Value<string>("type"),
                    t = f.Value<float?>("t") ?? 0, dur = f.Value<float?>("dur") ?? 0,
                    amount = f.Value<float?>("amount") ?? 0.5f,
                    atk = f.Value<float?>("atk") ?? -1f, rel = f.Value<float?>("rel") ?? -1f,
                    x = f.Value<float?>("x") ?? 0, y = f.Value<float?>("y") ?? 0,
                    x2 = f.Value<float?>("x2") ?? 0, y2 = f.Value<float?>("y2") ?? 0,
                    scale = f.Value<float?>("scale") ?? 1f,
                    alpha = f.Value<float?>("alpha") ?? -1f,
                    amp = f.Value<float?>("amp") ?? 0, hz = f.Value<float?>("hz") ?? 34f,
                    peak = f.Value<float?>("peak") ?? 1f,
                    falloff = f.Value<float?>("falloff") ?? 2f,
                    groundY = f.Value<float?>("groundY") ?? 0,
                    power = f.Value<float?>("power") ?? 260f,
                    seed = f.Value<float?>("seed") ?? 0,
                    count = f.Value<int?>("count") ?? 16,
                    flip = f.Value<bool?>("flip") ?? false,
                    color = f.Value<string>("color"), dir = f.Value<string>("dir"),
                    a = f.Value<string>("a"), b = f.Value<string>("b"),
                    tint = f.Value<string>("tint"),
                    ax = f.Value<float?>("ax") ?? 0, ay = f.Value<float?>("ay") ?? 0,
                    bx = f.Value<float?>("bx") ?? 0, by = f.Value<float?>("by") ?? 0,
                    w = f.Value<float?>("w") ?? 2f, sag = f.Value<float?>("sag") ?? 3f,
                };
                s.fx.Add(fd);
            }
            foreach (JObject c in o["sfx"]!)
                s.sfx.Add(new CueData { t = c.Value<float>("t"), label = c.Value<string>("label") });
            foreach (JObject c in o["cuts"]!)
                s.cuts.Add(new CutData { t = c.Value<float>("t"), label = c.Value<string>("label"), note = c.Value<string>("note") });
            foreach (var kv in (JObject)o["stageTracks"]!)
                s.stageTracks[kv.Key] = Keys(kv.Value);
            foreach (JObject l in o["lines"]!)
            {
                LineData ld = new()
                {
                    idx = l.Value<int>("idx"),
                    t = l.Value<float>("t"), dur = l.Value<float>("dur"),
                    lead = l.Value<float?>("lead") ?? 0.12f, cps = l.Value<float?>("cps") ?? 0.02f,
                    speaker = l.Value<string>("speaker"), text = l.Value<string>("text"),
                    raw = l.Value<string>("raw"), kind = l.Value<string>("kind") ?? "bubble",
                    actor = l.Value<string>("actor"),
                    below = l.Value<bool?>("below") ?? false,
                    glitch = l.Value<bool?>("glitch") ?? false,
                    pauses = new List<PauseData>(), spans = new List<SpanData>(),
                };
                if (l["at"] is JArray at && at.Count >= 2)
                    ld.at = new[] { at.Value<float>(0), at.Value<float>(1) };
                foreach (JObject p in l["pauses"]!)
                    ld.pauses.Add(new PauseData { i = p.Value<int>("i"), d = p.Value<float>("d") });
                if (l["spans"] is JArray spans)
                    foreach (JObject sp in spans)
                        ld.spans.Add(new SpanData
                        {
                            type = sp.Value<string>("type"),
                            i0 = sp.Value<int>("i0"), i1 = sp.Value<int>("i1"),
                            param = sp.Value<string>("param"),
                        });
                s.lines.Add(ld);
            }
            return s;
        }

        static List<Key> Keys(JToken tok)
        {
            List<Key> list = new();
            if (tok == null) return list;
            foreach (JObject k in tok)
            {
                float v;
                JToken jv = k["v"];
                if (jv!.Type == JTokenType.Boolean) v = jv.Value<bool>() ? 1f : 0f;
                else v = jv.Value<float>();
                list.Add(new Key { t = k.Value<float>("t"), v = v, ease = k.Value<string>("ease") });
            }
            return list;
        }
    }

    /// <summary>웹 engine.js와 동일한 트랙 평가 + 시드 랜덤.</summary>
    public static class WebEval
    {
        public static float Ease(string e, float u)
        {
            switch (e)
            {
                case "l": return u;
                case "ei": return u * u;
                case "eo": return 1f - (1f - u) * (1f - u);
                case "ec": return 1f - Mathf.Pow(1f - u, 3f);
                case "eq": return 1f - Mathf.Pow(1f - u, 5f);
                default: return u < 0.5f ? 2f * u * u : 1f - Mathf.Pow(-2f * u + 2f, 2f) / 2f; // eio
            }
        }

        public static float Num(List<Key> keys, float t, float dflt)
        {
            if (keys == null || keys.Count == 0) return dflt;
            if (t <= keys[0].t) return keys[0].v;
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                if (t >= keys[i].t)
                {
                    if (i + 1 >= keys.Count) return keys[i].v;
                    Key a = keys[i], b = keys[i + 1];
                    float u = b.t <= a.t ? 1f : Mathf.Clamp01((t - a.t) / (b.t - a.t));
                    return a.v + (b.v - a.v) * Ease(b.ease, u);
                }
            }
            return dflt;
        }

        public static StepKeyS Step(List<StepKeyS> keys, float t)
        {
            StepKeyS cur = keys[0];
            for (int i = 0; i < keys.Count; i++)
            {
                if (t >= keys[i].t) cur = keys[i];
                else break;
            }
            return cur;
        }

        /// <summary>engine.js rnd(s): frac(sin(s*127.1+311.7)*43758.5453) — double 정밀도로 동일 계산.</summary>
        public static float Rnd(double s)
        {
            double x = Math.Sin(s * 127.1 + 311.7) * 43758.5453;
            return (float)(x - Math.Floor(x));
        }

        public static float Srnd(double s) => Rnd(s) * 2f - 1f;

        /// <summary>stage.js rnd(s): frac(sin(s*127.1)*43758.5453) — debris 등 무대 계열 시드.</summary>
        public static float RndStage(double s)
        {
            double x = Math.Sin(s * 127.1) * 43758.5453;
            return (float)(x - Math.Floor(x));
        }

        /// <summary>타이핑 정지([T:]) 반영한 노출 글자 수 — engine.js typedCount와 동일.</summary>
        public static int TypedCount(LineData l, float e)
        {
            if (e <= 0f) return 0;
            float acc = 0f;
            for (int k = 0; k < l.pauses.Count; k++)
            {
                PauseData p = l.pauses[k];
                if (e < p.i * l.cps + acc + p.d)
                    return Mathf.Max(0, Mathf.Min(p.i, Mathf.FloorToInt((e - acc) / l.cps)));
                acc += p.d;
            }
            return Mathf.Max(0, Mathf.Min(l.text.Length, Mathf.FloorToInt((e - acc) / l.cps)));
        }
    }
}
