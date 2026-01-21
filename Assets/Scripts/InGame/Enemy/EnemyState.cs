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
}
