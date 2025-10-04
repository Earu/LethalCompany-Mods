using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace LethalCompany
{
    internal class ShadowDemon : EnemyAI
    {
		// Enemy states
		private enum DemonState { Idle, Chasing, Attacking }
		private DemonState currentState = DemonState.Idle;

		// Enemy settings
		public float detectionRange = 15f;
		public float attackRange = 2.5f;
		public int damage = 30;
		public float attackCooldown = 2f;

		private float attackTimer = 0f;

		public override void Start()
		{
			base.Start();
			this.currentState = DemonState.Idle;
		}

		public override void DoAIInterval()
		{
			base.DoAIInterval();

			// Reduce cooldown timer
			if (attackTimer > 0f) attackTimer -= base.AIIntervalTime;

			// Find nearest player
			TargetClosestPlayer();

			switch (currentState)
			{
				case DemonState.Idle:
					if (targetPlayer != null &&
						Vector3.Distance(targetPlayer.transform.position, transform.position) < detectionRange)
					{
						currentState = DemonState.Chasing;
					}
					break;

				case DemonState.Chasing:
					if (targetPlayer == null)
					{
						currentState = DemonState.Idle;
						break;
					}

					float dist = Vector3.Distance(targetPlayer.transform.position, transform.position);

					if (dist > detectionRange * 1.5f)
					{
						// Lost the player
						currentState = DemonState.Idle;
					}
					else if (dist <= attackRange && attackTimer <= 0f)
					{
						currentState = DemonState.Attacking;
					}
					else
					{
						// Move toward player
						SetDestinationToPosition(targetPlayer.transform.position);
					}
					break;

				case DemonState.Attacking:
					if (targetPlayer != null)
					{
						AttackPlayer(targetPlayer);
					}
					attackTimer = attackCooldown;
					currentState = DemonState.Chasing;
					break;
			}
		}

		private void AttackPlayer(PlayerControllerB player)
		{
			// Deal damage (game’s method for damaging a player)
			if (player != null && !player.isPlayerDead)
			{
				player.DamagePlayer(damage, causeOfDeath: CauseOfDeath.Mauling);
				Debug.Log("Shadow Demon attacked " + player.playerUsername);
			}
		}

		// Called when hit by shovel or weapon
		public override void HitEnemy(int force, PlayerControllerB playerWhoHit = null, bool playHitSFX = true, int hitID = -1)
		{
		}
	}
}
