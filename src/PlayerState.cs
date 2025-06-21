using BoomerangFoo.Powerups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BoomerangFoo
{
    public class PlayerState : MonoBehaviour
    {
        // Shield
        public int shieldHits = ShieldPowerup.Instance.ShieldHits;
        public Renderer shieldRenderer = null;

        // Multiboomerang
        public int multiBoomerangSplit = MultiBoomerangPowerup.Instance.BoomerangSplit;

        // Decoy
        public int maxDecoyCount = DecoyPowerup.Instance.MaxDecoyCount;
        public bool reviveAsDecoy = DecoyPowerup.Instance.ReviveAsDecoy;

        public int decoyCounter = 0;

        // MoveFaster
        public float moveFasterAttackCooldown = MoveFasterPowerup.Instance.AttackCooldown;
        public float moveFasterDashForceMultiplier = MoveFasterPowerup.Instance.DashForceMultiplier;
        public float moveFasterDashDurationMultiplier = MoveFasterPowerup.Instance.DashDurationMultiplier;
        public float moveFasterDashCooldownMultiplier = MoveFasterPowerup.Instance.DashCooldownMultiplier;
        public float moveFasterMoveSpeedMultiplier = MoveFasterPowerup.Instance.MoveSpeedMultiplier;
        public float moveFasterTurnSpeed = MoveFasterPowerup.Instance.TurnSpeed;

        public float originalAttackCooldownDuration = 0.66f;
        public float originalLungeCooldownDuration = 0.5f;
        public float originalDashSpeed = 660f;
        public float originalDashCooldown = 0.55f;
        public float originalDashDuration = 0.55f;
        public float originalTurningSpeed = 12f;
        public float originalWalkSpeed = 200f;
        public float originalWalkSpeedUnarmed = 230f;

        // Bamboozle
        public bool reverseInputsImmunity = BamboozlePowerup.Instance.Immunity;
        public float reverseInputsDuration = BamboozlePowerup.Instance.Duration;

        // Flying
        public float flyingDuration = FlyingPowerup.Instance.Duration;
        public bool resetOnGround = FlyingPowerup.Instance.ResetOnGround;

        public float flyingTimeLimit;
        public float flyingTimer;
        public bool isFlyingTimerOn;
        public bool isFlying;
        public bool flyingForceFall;

        // Explosive
        public float explosiveRadiusMultiplier = 1f;
    }
}
