//using MLAPI.Messaging;
//using MLAPI.NetworkVariable;
using Unity.Netcode;
using UnityEngine;

namespace UTJ.NetcodeGameObjectSample
{
    /// <summary>
    /// キャラクターの動きのコントローラー
    /// </summary>
    public class CharacterMoveController : Unity.Netcode.NetworkBehaviour
    {
        public TextMesh playerNameTextMesh;
        public ParticleSystem soundPlayingParticle;
        public AudioSource audioSouceComponent;


        public AudioClip[] audios;

        private Rigidbody rigidbodyComponent;
        private Animator animatorComponent;

        // Networkで同期する変数を作成します
        #region NETWORKED_VAR
        // Animationに流すスピード変数
        private NetworkVariable<float> speed = new NetworkVariable<float>( 0.0f);
        // プレイヤー名
        private NetworkVariable<Unity.Collections.FixedString64Bytes> playerName = new NetworkVariable<Unity.Collections.FixedString64Bytes>();
        #endregion NETWORKED_VAR


        // NetworkVariableはサーバーでしか更新できないので更新を依頼します
        [Unity.Netcode.ServerRpc(RequireOwnership = true)]
        private void SetSpeedServerRpc(float speed)
        {
            this.speed.Value = speed;
        }
        [Unity.Netcode.ServerRpc(RequireOwnership = true)]
        private void SetPlayerNameServerRpc(string name)
        {
            this.playerName.Value = name;
        }

        private void Awake()
        {
            this.rigidbodyComponent = this.GetComponent<Rigidbody>();
            this.animatorComponent = this.GetComponent<Animator>();

            // Player名が変更になった時のコールバック指定
            this.playerName.OnValueChanged += OnChangePlayerName;

            // あとServer時に余計なものを削除します
#if UNITY_SERVER
            NetworkUtility.RemoveAllStandaloneComponents(this.gameObject);
#elif ENABLE_AUTO_CLIENT
            if (NetworkUtility.IsBatchModeRun)
            {
                NetworkUtility.RemoveAllStandaloneComponents(this.gameObject);
            }
#endif
        }

        /// <summary>
        /// Start
        /// </summary>
        private void Start()
        {
            if (IsOwner)
            {
                // プレイヤー名をセットします
                SetPlayerNameServerRpc( ConfigureConnectionBehaviour.playerName);
                // コントローラーの有効化をします
                ControllerBehaviour.Instance.Enable();

                ViewCamera = GameObject.Find("Main Camera");
            }
        }

        /// <summary>
        /// OnDestroy
        /// </summary>
        private new void OnDestroy()
        {
            base.OnDestroy();
            if (IsOwner)
            {
                // コントローラーの無効化をします
                if (ControllerBehaviour.Instance)
                {
                    ControllerBehaviour.Instance.Disable();
                }
            }
        }

        /// <summary>
        /// player名変更のコールバック
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="current"></param>
        void OnChangePlayerName(Unity.Collections.FixedString64Bytes prev,
            Unity.Collections.FixedString64Bytes current)
        {
            if (playerNameTextMesh != null)
            {
                playerNameTextMesh.text = current.Value;
            }
        }

        /// <summary>
        /// Update
        /// </summary>
        void Update()
        {
            // TODO:::なんか OnValueChangedがおかしい…。
            // 自分より前にSpawnされた人の名前取れないんで Workaround
            playerNameTextMesh.text = this.playerName.Value.Value;
            // Animatorの速度更新(歩き・走り・静止などをSpeedでコントロールしてます)
            animatorComponent.SetFloat("Speed", speed.Value);
            // 音量調整
            this.audioSouceComponent.volume = SoundVolume.VoiceValue;

            // オーナーとして管理している場合、ここのUpdateを呼びます
            if (IsOwner)
            {
                UpdateAsOwner();
                CameraUpdate();
            }
        }


        /// <summary>
        /// オーナーとしての処理
        /// </summary>
        private void UpdateAsOwner()
        {
            // 移動処理
            Vector3 move = ControllerBehaviour.Instance.LPadVector;
            float speedValue = move.magnitude;
            this.SetSpeedServerRpc(speedValue);
            move *= Time.deltaTime * 4.0f;
            rigidbodyComponent.position += move;

            // 移動している方角に向きます
            if (move.sqrMagnitude > 0.00001f)
            {
                rigidbodyComponent.rotation = Quaternion.LookRotation(move, Vector3.up);
            }
            // 底に落ちたら適当に復帰します。
            if (transform.position.y < -10.0f)
            {
                var randomPosition = new Vector3(Random.Range(-7, 7), 5.0f, Random.Range(-7, 7));
                transform.position = randomPosition;
            }
            // キーを押して音を流します
            for (int i = 0; i < this.audios.Length; ++i)
            {
                if (ControllerBehaviour.Instance.IsKeyDown(i))
                {
                    // 他の人に流してもらうために、サーバーにRPCします。
                    PlayAudioRequestOnServerRpc(i);
                }
            }
            // 入力の通知を通知します
            ControllerBehaviour.Instance.OnUpdateEnd();
        }

        /// <summary>
        /// Clientからサーバーに呼び出されるRPCです。
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="serverRpcParams"></param>
        [Unity.Netcode.ServerRpc(RequireOwnership = true)]
        private void PlayAudioRequestOnServerRpc(int idx,ServerRpcParams serverRpcParams = default)
        {
            // PlayAudioを呼び出します
            PlayAudioClientRpc(idx);
        }

        /// <summary>
        /// 音を再生します。付随してParticleをPlayします
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="clientRpcParams"></param>
        [Unity.Netcode.ClientRpc]
        private void PlayAudioClientRpc(int idx,ClientRpcParams clientRpcParams = default)
        {
            PlayAudio(idx);
        }

        /// <summary>
        /// PlayAudio
        /// </summary>
        /// <param name="idx"></param>
        private void PlayAudio(int idx) { 
            this.audioSouceComponent.clip = audios[idx];
            this.audioSouceComponent.Play();

            this.soundPlayingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var mainModule = soundPlayingParticle.main;
            mainModule.duration = audios[idx].length;

            this.soundPlayingParticle.Play();
        }


        //_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/

        private bool mFloorTouched = false;

        public AudioClip HitSound = null;
        public AudioClip CoinSound = null;

        public GameObject ViewCamera = null;

        /// <summary>
        /// CameraUpdate
        /// </summary>
        protected void CameraUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            if (ViewCamera != null)
            {
                var direction = (Vector3.up * 4.0f + Vector3.back * 6.0f) * 2.5f;
                ViewCamera.transform.position = transform.position + direction;
                ViewCamera.transform.LookAt(transform.position);
            }
        }

        /// <summary>
        /// OnCollisionEnter
        /// </summary>
        /// <param name="coll"></param>
        void OnCollisionEnter(Collision coll)
        {
            if (coll.gameObject.tag.Equals("Floor"))
            {
                mFloorTouched = true;
                if (audioSouceComponent != null && HitSound != null && coll.relativeVelocity.y > .5f)
                {
                    audioSouceComponent.PlayOneShot(HitSound, coll.relativeVelocity.magnitude);
                }
            }
            else
            {
                if (audioSouceComponent != null && HitSound != null && coll.relativeVelocity.magnitude > 2f)
                {
                    audioSouceComponent.PlayOneShot(HitSound, coll.relativeVelocity.magnitude);
                }
            }
        }

        /// <summary>
        /// OnCollisionExit
        /// </summary>
        /// <param name="coll"></param>
        void OnCollisionExit(Collision coll)
        {
            if (coll.gameObject.tag.Equals("Floor"))
            {
                mFloorTouched = false;
            }
        }

        /// <summary>
        /// OnTriggerEnter
        /// </summary>
        /// <param name="other"></param>
        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag.Equals("Coin"))
            {
                if (audioSouceComponent != null && CoinSound != null)
                {
                    audioSouceComponent.PlayOneShot(CoinSound);
                }
                Destroy(other.gameObject);
            }
        }
    }
}