using System;
using System.Collections.Generic;
using Core;
using fsm;
using Game;
using UnityEngine;

public class SeinChargeFlameAbility : CharacterState, ISeinReceiver
{
	public float ChargeDuration
	{
		get
		{
			return this.ChargeFlameSettings.ChargeDuration;
		}
	}

	public bool HasEnoughEnergy
	{
		get
		{
			return this.m_sein.Energy.CanAfford(this.m_sein.PlayerAbilities.ChargeFlameEfficiency.HasAbility ? 0f : 0.5f);
		}
	}

	public void SpendEnergy()
	{
		this.m_sein.Energy.Spend(this.m_sein.PlayerAbilities.ChargeFlameEfficiency.HasAbility ? 0f : 0.5f);
	}

	public void RestoreEnergy()
	{
		this.m_sein.Energy.Gain(this.m_sein.PlayerAbilities.ChargeFlameEfficiency.HasAbility ? 0f : 0.5f);
	}

	public override void Awake()
	{
		base.Awake();
		this.State.Start = new State
		{
			UpdateStateEvent = new Action(this.UpdateStartState),
			OnEnterEvent = new Action(this.OnEnterStartState)
		};
		this.State.Precharging = new State
		{
			UpdateStateEvent = new Action(this.UpdatePrechargingState)
		};
		this.State.Charging = new State
		{
			UpdateStateEvent = new Action(this.UpdateChargingState)
		};
		this.State.Charged = new State
		{
			UpdateStateEvent = new Action(this.UpdateChargedState),
			OnEnterEvent = new Action(this.OnEnterChargedState)
		};
		this.Logic.RegisterStates(new IState[]
		{
			this.State.Start,
			this.State.Precharging,
			this.State.Charging,
			this.State.Charged
		});
		this.Logic.ChangeState(this.State.Start);
		Game.Checkpoint.Events.OnPostRestore.Add(new Action(this.OnRestoreCheckpoint));
	}

	public void OnRestoreCheckpoint()
	{
		if (this.m_chargeFlameChargeEffect)
		{
			InstantiateUtility.Destroy(this.m_chargeFlameChargeEffect);
		}
		if (this.CurrentChargingSound())
		{
			this.CurrentChargingSound().StopAndFadeOut(0.5f);
		}
		this.Logic.ChangeState(this.State.Start);
	}

	public void OnEnterStartState()
	{
		if (this.m_chargeFlameChargeEffect)
		{
			InstantiateUtility.Destroy(this.m_chargeFlameChargeEffect);
		}
	}

	public void UpdateStartState()
	{
		if (this.m_chargeFlameChargeEffect)
		{
			InstantiateUtility.Destroy(this.m_chargeFlameChargeEffect);
		}
		if (this.m_sein.Controller.IsBashing)
		{
			return;
		}

		bool pressed = this.ChargeFlameButton.OnPressed && !this.ChargeFlameButton.Used;

		if (RandomizerSettings.Controls.Autofire == RandomizerSettings.AutofireMode.Hold && !RandomizerRebinding.SuppressAutofire.Pressed)
		{
			pressed = false;
		}

		if (pressed && this.m_sein.PlayerAbilities.ChargeFlame.HasAbility && !this.m_sein.Controller.InputLocked && !this.m_sein.Abilities.SpiritFlame.LockShootingSpiritFlame)
		{
			this.Logic.ChangeState(this.State.Precharging);
		}
	}

	public void UpdatePrechargingState()
	{
		if (this.Logic.CurrentStateTime > 0.3f)
		{
			this.m_chargeFlameChargeEffect = (GameObject)InstantiateUtility.Instantiate(this.ChargeFlameSettings.ChargeFlameChargeEffectPrefab);
			this.m_chargeFlameChargeEffect.transform.position = Characters.Ori.transform.position;
			this.m_chargeFlameChargeEffect.transform.parent = Characters.Ori.transform;
			this.m_chargeFlameChargeEffect.GetComponentsInChildren<LegacyAnimator>(SeinChargeFlameAbility.s_legacyAnimatorList);
			for (int i = 0; i < SeinChargeFlameAbility.s_legacyAnimatorList.Count; i++)
			{
				SeinChargeFlameAbility.s_legacyAnimatorList[i].Speed = 1f / this.ChargeDuration;
			}
			SeinChargeFlameAbility.s_legacyAnimatorList.Clear();
			if (this.CurrentChargingSound())
			{
				this.CurrentChargingSound().Play();
			}
			this.Logic.ChangeState(this.State.Charging);
			return;
		}
		if (this.ChargeFlameButton.Released)
		{
			this.Logic.ChangeState(this.State.Start);
			return;
		}
		if (this.m_sein.Abilities.SpiritFlame.LockShootingSpiritFlame)
		{
			this.Logic.ChangeState(this.State.Start);
			return;
		}
		if (this.m_sein.Controller.InputLocked)
		{
			this.Logic.ChangeState(this.State.Start);
		}
	}

	public void UpdateChargingState()
	{
		if (this.ChargeFlameButton.Released || this.m_sein.Controller.InputLocked || this.m_sein.Abilities.SpiritFlame.LockShootingSpiritFlame)
		{
			if (this.CurrentChargingSound())
			{
				this.CurrentChargingSound().StopAndFadeOut(0.5f);
			}
			this.Logic.ChangeState(this.State.Start);
			return;
		}
		if (this.Logic.CurrentStateTime >= this.ChargeDuration)
		{
			if (this.HasEnoughEnergy)
			{
				this.Logic.ChangeState(this.State.Charged);
				this.SpendEnergy();
				return;
			}
			if (this.CurrentChargingSound())
			{
				this.CurrentChargingSound().StopAndFadeOut(0.5f);
			}
			this.Logic.ChangeState(this.State.Start);
			UI.SeinUI.ShakeEnergyOrbBar();
			if (this.NotEnoughEnergySound)
			{
				Sound.Play(this.NotEnoughEnergySound.GetSound(null), base.transform.position, null);
			}
		}
	}

	public void ReleaseChargeBurst()
	{
		if (this.CurrentChargingSound())
		{
			this.CurrentChargingSound().StopAndFadeOut(0.5f);
		}
		if (this.m_sein.PlayerAbilities.ChargeFlameBlast.HasAbility)
		{
			InstantiateUtility.Instantiate(this.ChargeFlameSettings.ChargeFlameBurstC, Characters.Ori.Position, Quaternion.identity);
		}
		else if (this.m_sein.PlayerAbilities.ChargeFlameBurn.HasAbility)
		{
			InstantiateUtility.Instantiate(this.ChargeFlameSettings.ChargeFlameBurstB, Characters.Ori.Position, Quaternion.identity);
		}
		else
		{
			InstantiateUtility.Instantiate(this.ChargeFlameSettings.ChargeFlameBurstA, Characters.Ori.Position, Quaternion.identity);
		}
		this.Logic.ChangeState(this.State.Start);
	}

	public void UpdateChargedState()
	{
		if (this.ChargeFlameButton.Released)
		{
			this.ReleaseChargeBurst();
			return;
		}

		if (Core.Input.SoulFlame.OnPressed)
		{
			Core.Input.SoulFlame.Used = true;
			if (this.CurrentChargingSound())
			{
				this.CurrentChargingSound().StopAndFadeOut(0.5f);
			}

			foreach (var item in this.m_capturedProjectiles)
			{
				if (!InstantiateUtility.IsDestroyed(item.Key as Component))
				{
					(item.Key as Component).GetComponent<Collider>().enabled = true;
				}
			}

			this.Logic.ChangeState(this.State.Start);
			this.RestoreEnergy();
			UI.SeinUI.ShakeEnergyOrbBar();
			return;
		}

		if (RandomizerBonus.EnhancedChargeFlame)
		{
			for (int i = 0; i < Targets.Attackables.Count; i++)
			{
				IAttackable attackable = Targets.Attackables[i];
				if (InstantiateUtility.IsDestroyed(attackable as Component))
				{
					continue;
				}

				if (attackable.CanBeChargeFlamed() && attackable is Projectile)
				{
					Vector3 distance = attackable.Position - Characters.Ori.transform.position;
					if (distance.magnitude <= this.m_captureRadius)
					{
						SeinChargeFlameAbility.CapturedProjectile capturedProjectile = null;

						if (!this.m_capturedProjectiles.ContainsKey(attackable))
						{
							capturedProjectile = new SeinChargeFlameAbility.CapturedProjectile();
							this.m_capturedProjectiles[attackable] = capturedProjectile;
						}
						else
						{
							capturedProjectile = this.m_capturedProjectiles[attackable];
							if (!capturedProjectile.IsDestroyed)
							{
								continue;
							}
						}

						capturedProjectile.Direction = distance.normalized;
						capturedProjectile.CapturedVelocity = (attackable as Projectile).Speed;
						capturedProjectile.IsDestroyed = false;
						(attackable as Component).GetComponent<Collider>().enabled = false;
					}
				}
			}

			foreach (var item in this.m_capturedProjectiles)
			{
				if (InstantiateUtility.IsDestroyed(item.Key as Component))
				{
					item.Value.IsDestroyed = true;
					continue;
				}

				Projectile projectile = item.Key as Projectile;
				Vector3 targetPosition = Characters.Ori.transform.position;
				Vector3 direction = targetPosition - projectile.Position;

				if (direction.magnitude > 0.2f)
				{
					projectile.Direction = (projectile.Direction + direction).normalized;
					projectile.Speed = Mathf.Lerp(1f, 30f, direction.magnitude / this.m_captureRadius);
				}
				else
				{
					projectile.SpeedVector = Vector3.zero;
					projectile.Position = targetPosition;
				}
			}
		}
	}

	public Core.Input.InputButtonProcessor ChargeFlameButton
	{
		get
		{
			return Core.Input.SpiritFlame;
		}
	}

	public bool IsCharging
	{
		get
		{
			return this.Logic.CurrentState != this.State.Start;
		}
	}

	public override void UpdateCharacterState()
	{
		this.Logic.UpdateState(Time.deltaTime);
	}

	public void SetReferenceToSein(SeinCharacter sein)
	{
		this.m_sein = sein;
		this.m_sein.Abilities.ChargeFlame = this;
	}

	public override void OnExit()
	{
		if (this.Logic.CurrentState == this.State.Precharging)
		{
			this.Logic.ChangeState(this.State.Start);
		}
		if (this.Logic.CurrentState == this.State.Charging)
		{
			if (this.CurrentChargingSound())
			{
				this.CurrentChargingSound().StopAndFadeOut(0.5f);
			}
			this.Logic.ChangeState(this.State.Start);
		}
		if (this.Logic.CurrentState == this.State.Charged)
		{
			this.ReleaseChargeBurst();
		}
		base.OnExit();
	}

	private SoundSource CurrentChargingSound()
	{
		if (this.m_sein.PlayerAbilities.ChargeFlameBlast.HasAbility)
		{
			return this.ChargingSoundLevelC;
		}
		if (this.m_sein.PlayerAbilities.ChargeFlameBurn.HasAbility)
		{
			return this.ChargingSoundLevelB;
		}
		return this.ChargingSoundLevelA;
	}

	public Dictionary<IAttackable, CapturedProjectile> CapturedProjectiles
	{
		get
		{
			return this.m_capturedProjectiles;
		}
	}

	public void OnEnterChargedState()
	{
		this.m_capturedProjectiles.Clear();
	}

	public SoundSource ChargingSoundLevelA;

	public SoundSource ChargingSoundLevelB;

	public SoundSource ChargingSoundLevelC;

	public AchievementAsset KillEnemiesSimultaneouslyAchievement;

	public SoundProvider NotEnoughEnergySound;

	public SeinChargeFlameAbility.ChargeFlameDefinitions ChargeFlameSettings;

	public SeinChargeFlameAbility.States State = new SeinChargeFlameAbility.States();

	private StateMachine Logic = new StateMachine();

	private GameObject m_chargeFlameChargeEffect;

	public float EnergyCost = 1f;

	private static readonly List<LegacyAnimator> s_legacyAnimatorList = new List<LegacyAnimator>();

	private SeinCharacter m_sein;

	private float m_captureRadius = 9f;

	private Dictionary<IAttackable, CapturedProjectile> m_capturedProjectiles = new Dictionary<IAttackable, CapturedProjectile>();

	[Serializable]
	public class ChargeFlameDefinitions
	{
		public float ChargeDuration = 1f;

		public GameObject ChargeFlameBurstA;

		public GameObject ChargeFlameBurstB;

		public GameObject ChargeFlameBurstC;

		public GameObject ChargeFlameChargeEffectPrefab;
	}

	public class States
	{
		public State Start;

		public State Precharging;

		public State Charging;

		public State Charged;
	}

	public class CapturedProjectile
	{
		public Vector3 Direction;

		public float CapturedVelocity;

		public bool IsDestroyed;
	}
}
