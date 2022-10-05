using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
<<<<<<< Updated upstream
=======
		public bool basicAttack;
		public bool specialAttackE;
		public bool specialAttackR;
		public bool specialAttackF;
>>>>>>> Stashed changes

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
<<<<<<< Updated upstream
=======

		public void OnBasicAttack(InputValue value)
        {
			BasicAttackInput(value.isPressed);
        }

		public void OnSpecialAttackE(InputValue value)
		{
			SpecialAttackEInput(value.isPressed);
		}
		public void OnSpecialAttackF(InputValue value)
		{
			SpecialAttackFInput(value.isPressed);
		}
		public void OnSpecialAttackR(InputValue value)
		{
			SpecialAttackRInput(value.isPressed);
		}
>>>>>>> Stashed changes
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

<<<<<<< Updated upstream
=======
		public void BasicAttackInput(bool newBasicAttackState)
        {
			basicAttack = newBasicAttackState;
        }

		public void SpecialAttackEInput(bool newSpecialAttackState)
		{
			specialAttackE = newSpecialAttackState;
		}

		public void SpecialAttackRInput(bool newSpecialAttackState)
		{
			specialAttackR = newSpecialAttackState;
		}

		public void SpecialAttackFInput(bool newSpecialAttackState)
		{
			specialAttackF = newSpecialAttackState;
		}

>>>>>>> Stashed changes
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}