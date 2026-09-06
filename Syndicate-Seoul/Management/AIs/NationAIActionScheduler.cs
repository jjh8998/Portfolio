using System.Collections.Generic;
using UnityEngine;

public class NationAIActionScheduler : MonoBehaviour
{
    [SerializeField] private int maxActionsPerFrame = 1;

    private readonly Queue<NationAIController> pendingControllers = new Queue<NationAIController>();
    private readonly HashSet<NationAIController> queuedControllers = new HashSet<NationAIController>();

    public int PendingCount => pendingControllers.Count;

    public bool Enqueue(NationAIController _controller)
    {
        if (_controller == null)
            return false;

        if (queuedControllers.Contains(_controller))
            return false;

        pendingControllers.Enqueue(_controller);
        queuedControllers.Add(_controller);
        return true;
    }

    private void OnValidate()
    {
        maxActionsPerFrame = Mathf.Max(1, maxActionsPerFrame);
    }

    private void Update()
    {
        int actionLimit = Mathf.Max(1, maxActionsPerFrame);
        int executedCount = 0;

        while (executedCount < actionLimit && pendingControllers.Count > 0)
        {
            NationAIController controller = pendingControllers.Dequeue();
            queuedControllers.Remove(controller);

            if (controller == null || !controller.isActiveAndEnabled)
                continue;

            if (!controller.CanThinkAndAct())
                continue;

            controller.ThinkAndAct();
            executedCount++;
        }
    }
}
