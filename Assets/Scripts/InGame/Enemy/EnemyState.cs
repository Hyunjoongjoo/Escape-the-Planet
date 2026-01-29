using UnityEngine;

public class EnemyState : MonoBehaviour
{
    public enum State
    {
        Idle,
        Move,
        Hit,
        Dead
    }

    public State current = State.Idle;

    public void ChangeState(State next)
    {
        current = next;
    }
    public void SetRemoteState(State next)
    {
        if (current == State.Dead)
        {
            return;
        }

        current = next;
    }
}
