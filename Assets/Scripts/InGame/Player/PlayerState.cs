using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum State
    {
        Idle,
        Move,
        Attack,
        Hit,
        Dead
    }

    public State current = State.Idle;

    public void ChangeState(State newState)
    {
        current = newState;

        switch (newState)
        {
            case State.Idle:
                break;
            case State.Move:
                break;
            case State.Attack:
                break;
            case State.Hit:
                break;
            case State.Dead:
                break;
        }
    }
}
