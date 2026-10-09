using UnityEngine;

// Процедурная анимация ходьбы/бега/приседа для моделей Floreswa (скелет Rigify metarig).
// Готовых клипов у моделей нет, поэтому кости крутятся кодом по скорости перемещения объекта.
// Работает одинаково для своего игрока и для чужих: скорость считается по смещению transform.
public class ProceduralWalk : MonoBehaviour
{
    [Header("Шаг")]
    [Tooltip("Сколько метров проходит персонаж за полный цикл (два шага) при ходьбе")]
    public float walkCycleLength = 1.6f;
    [Tooltip("То же при беге")]
    public float runCycleLength = 2.6f;
    [Tooltip("Скорость, начиная с которой анимация полностью «бег»")]
    public float runSpeed = 6f;
    [Tooltip("Скорость, при которой ходьба проигрывается с полной амплитудой")]
    public float walkSpeed = 3f;

    [Header("Амплитуды (градусы)")]
    public float walkLegSwing = 28f;
    public float runLegSwing = 45f;
    public float walkKneeBend = 35f;
    public float runKneeBend = 80f;
    public float walkArmSwing = 22f;
    public float runArmSwing = 50f;
    public float elbowBend = 12f;
    public float runElbowBend = 70f;
    public float runLean = 8f;
    [Tooltip("Насколько опустить руки из A-позы к телу")]
    public float armsDown = 25f;

    [Header("Присед")]
    public float crouchKneeAngle = 65f;
    public float crouchSpeed = 6f;

    [Tooltip("Присед для чужих игроков выставляет PositionSync; для своего берётся из PlayerController")]
    public bool crouching;

    Transform hips, spine1, thighL, thighR, shinL, shinR, footL, footR, armL, armR, foreL, foreR;
    Transform[] bones;
    Quaternion[] restRot;
    Vector3 hipsRestPos;
    float legLength; // в локальных единицах корня модели

    PlayerController player;
    Vector3 lastPos;
    float speed, forwardSign = 1f, phase, crouchBlend;

    void Awake()
    {
        hips = Find("spine");
        spine1 = Find("spine.001");
        thighL = Find("thigh.L"); thighR = Find("thigh.R");
        shinL = Find("shin.L"); shinR = Find("shin.R");
        footL = Find("foot.L"); footR = Find("foot.R");
        armL = Find("upper_arm.L"); armR = Find("upper_arm.R");
        foreL = Find("forearm.L"); foreR = Find("forearm.R");

        bones = new[] { hips, spine1, thighL, thighR, shinL, shinR, footL, footR, armL, armR, foreL, foreR };
        foreach (var b in bones)
            if (b == null)
            {
                Debug.LogWarning($"[ProceduralWalk] {name}: не найдены кости metarig, анимация отключена");
                enabled = false;
                return;
            }

        restRot = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++) restRot[i] = bones[i].localRotation;
        hipsRestPos = hips.localPosition;

        Vector3 t = transform.InverseTransformPoint(thighL.position);
        Vector3 s = transform.InverseTransformPoint(shinL.position);
        Vector3 f = transform.InverseTransformPoint(footL.position);
        legLength = ((t - s).magnitude + (s - f).magnitude) * 0.5f;

        player = GetComponentInParent<PlayerController>();
        lastPos = transform.position;
    }

    Transform Find(string boneName)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == boneName) return t;
        return null;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Скорость по смещению — так одинаково работает и для локального, и для сетевого персонажа
        Vector3 delta = transform.position - lastPos;
        lastPos = transform.position;
        delta.y = 0;
        float rawSpeed = delta.magnitude / dt;
        if (rawSpeed > 20f) rawSpeed = 0; // телепорт/респаун
        speed = Mathf.Lerp(speed, rawSpeed, 1f - Mathf.Exp(-10f * dt));
        if (rawSpeed > 0.1f)
            forwardSign = Vector3.Dot(delta, transform.forward) < -0.01f ? -1f : 1f;

        if (player != null) crouching = player.IsCrouching;
        crouchBlend = Mathf.MoveTowards(crouchBlend, crouching ? 1f : 0f, crouchSpeed * dt);

        float move = Mathf.Clamp01(speed / walkSpeed);                      // 0 — стоит, 1 — идёт
        float run = Mathf.Clamp01((speed - walkSpeed) / (runSpeed - walkSpeed)); // 0 — шаг, 1 — бег

        float cycle = Mathf.Lerp(walkCycleLength, runCycleLength, run);
        phase = Mathf.Repeat(phase + forwardSign * speed / cycle * Mathf.PI * 2f * dt, Mathf.PI * 2f);
        // Когда персонаж останавливается, ноги плавно возвращаются в стойку за счёт move → 0

        float sin = Mathf.Sin(phase), cos = Mathf.Cos(phase);

        float legSwing = Mathf.Lerp(walkLegSwing, runLegSwing, run) * move;
        float knee = Mathf.Lerp(walkKneeBend, runKneeBend, run) * move;
        float armSwing = Mathf.Lerp(walkArmSwing, runArmSwing, run) * move;
        float elbow = Mathf.Lerp(elbowBend, runElbowBend, run);

        // Знак угла: «+» наклоняет верх кости вперёд (для ноги — отводит стопу назад)
        float thighLA = -legSwing * sin;
        float thighRA = legSwing * sin;
        float shinLA = knee * Mathf.Max(0f, cos) + 5f * move;
        float shinRA = knee * Mathf.Max(0f, -cos) + 5f * move;

        // Присед
        float crouchA = crouchKneeAngle * crouchBlend;
        thighLA -= crouchA; thighRA -= crouchA;
        shinLA += crouchA * 2f; shinRA += crouchA * 2f;

        // Сброс в исходную позу
        for (int i = 0; i < bones.Length; i++) bones[i].localRotation = restRot[i];
        hips.localPosition = hipsRestPos;

        Vector3 right = transform.right;
        Vector3 fwd = transform.forward;

        // Таз: опускается в присяде и чуть покачивается при шаге
        float drop = 2f * legLength * (1f - Mathf.Cos(crouchA * Mathf.Deg2Rad));
        float bob = Mathf.Abs(cos) * 0.03f * legLength * move;
        hips.position -= transform.up * ((drop + bob) * transform.lossyScale.y);

        Rotate(spine1, right, runLean * run + crouchA * 0.45f);
        Rotate(thighL, right, thighLA);
        Rotate(thighR, right, thighRA);
        Rotate(shinL, right, shinLA);
        Rotate(shinR, right, shinRA);
        // Стопа остаётся примерно параллельной полу
        Rotate(footL, right, -(thighLA + shinLA) * 0.8f);
        Rotate(footR, right, -(thighRA + shinRA) * 0.8f);

        // Руки: опустить из A-позы, затем махать в противофазе ногам
        Rotate(armL, fwd, armsDown);
        Rotate(armR, fwd, -armsDown);
        Rotate(armL, right, armSwing * sin);
        Rotate(armR, right, -armSwing * sin);
        Rotate(foreL, right, -elbow);
        Rotate(foreR, right, -elbow);
    }

    static void Rotate(Transform bone, Vector3 axis, float angle)
    {
        bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;
    }
}
