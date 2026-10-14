using ProjectM.Core;
using ProjectM.Data;
using ProjectM.Presentation;
using UnityEngine;

namespace ProjectM
{
    /// <summary>
    /// 씬 컴포지션 루트. 순수 로직 객체를 만들고 표현 어댑터에 연결한다 (CLAUDE.md: 어댑터는 씬 컴포지션 루트 하나에서 연결).
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        [SerializeField] private GameConfigAsset _config;
        [SerializeField] private TilemapPresenter _tilemapPresenter;
        [SerializeField] private EntityPresenter _entityPresenter;
        [SerializeField] private CameraController _camera;

        [Header("1-3 확인용 임시 방 — 1-5 맵 생성으로 교체")]
        [SerializeField] private int _previewRoomWidth = 12;
        [SerializeField] private int _previewRoomHeight = 8;

        public SimEventBus Events { get; private set; }
        public GameConfig Config { get; private set; }

        private void Awake()
        {
            if (_config == null || _tilemapPresenter == null || _entityPresenter == null)
            {
                Debug.LogError("[GameRoot] 참조가 비어 있다. 메뉴 ProjectM/Setup/Main Scene을 실행한다.", this);
                enabled = false;
                return;
            }

            Config = _config.ToConfig();
            Events = new SimEventBus();
            _tilemapPresenter.Bind(Events);
            _entityPresenter.Bind(Events);
        }

        private void Start()
        {
            if (Events == null) return;
            ShowPreviewRoom();
        }

        // TODO(1-5): 맵 생성 결과로 교체하고 이 메서드와 _previewRoom* 필드를 지운다.
        private void ShowPreviewRoom()
        {
            var map = new GridMap(_previewRoomWidth, _previewRoomHeight);
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    bool border = x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1;
                    map.Set(new GridPos(x, y), border ? TileType.Wall : TileType.Floor);
                }
            }

            var ids = new EntityIdAllocator();
            var center = new GridPos(map.Width / 2, map.Height / 2);
            Events.Publish(new MapLoadedEvent(map));
            Events.Publish(new EntitySpawnedEvent(ids.Allocate(), center, "player"));
            Events.Flush();

            if (_camera != null)
                _camera.SnapToTarget(_entityPresenter.CellCenter(center));
        }
    }
}
