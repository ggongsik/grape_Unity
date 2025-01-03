﻿using System.Collections;
using UnityEngine;

// 총을 구현
public class Gun : MonoBehaviour
{
    // 총의 상태를 표현하는 데 사용할 타입을 선언
    public enum State
    {
        Ready, // 발사 준비됨
        Empty, // 탄알집이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 탄알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    private LineRenderer bulletLineRenderer; // 탄알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기

    public GunData gunData; // 총의 현재 데이터

    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄알
    public int magAmmo; // 현재 탄알집에 남아 있는 탄알

    private float lastFireTime; // 총을 마지막으로 발사한 시점

    private void Awake() {
        // 사용할 컴포넌트의 참조 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();
        //사용할 점을 두 개로 변경
        bulletLineRenderer.positionCount = 2;
        //라인 렌더러 비활성화
        bulletLineRenderer.enabled = false;
    }

    private void OnEnable() {
        // 총 상태 초기화
        ammoRemain = gunData.startAmmoRemain;
        magAmmo = gunData.magCapacity;

        //발사 가능상태로  변경
        state = State.Ready;
        //발사시점 초기화
        lastFireTime = 0;
    }

    // 발사 시도
    public void Fire()
    {
        if (state == State.Ready && Time.time >= lastFireTime + gunData.timeBetFire)
        {
            lastFireTime = Time.time;
            Shot();
        }
    }
    // 실제 발사 처리
    private void Shot()
    {
        //레이캐스트 결과를 담은 컨테이너
        RaycastHit hit;

        //총알이 맞은 곳을 저장하느 변수
        Vector3 hitPosition = Vector3.zero;

        //레ㅣ캐스트 시작
        if (Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, fireDistance))
        {
            //레이가 충돌했을 경우

            //충돌한 대상으로부터 IDamage 가져오기
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            //IDamage 가져오기 성공
            if (target != null)
            {
                target.OnDamage(gunData.damage, hit.point, hit.normal);
            }
            hitPosition = hit.point;
        }
        else //레이 충돌 하지 않을경우
        {
            hitPosition = fireTransform.position + fireTransform.forward * fireDistance;
        }//발사 이펙트
        StartCoroutine(ShotEffect(hitPosition));
        //탄알수 -1
        magAmmo--;
        if( magAmmo <= 0)
        {
            state = State.Empty;
        }
    }
    // 발사 이펙트와 소리를 재생하고 탄알 궤적을 그림
    private IEnumerator ShotEffect(Vector3 hitPosition) {
        //총구화염
        muzzleFlashEffect.Play();
        //탄피배출
        shellEjectEffect.Play();
        //총소리
        gunAudioPlayer.PlayOneShot(gunData.shotClip);
        //라인 렌더러를 총구 위치로 조정
        bulletLineRenderer.SetPosition(0,fireTransform.position);
        //렌더러의 끝점은 충돌 위치 까지
        bulletLineRenderer.SetPosition(1,hitPosition);
        
        // 라인 렌더러를 활성화하여 탄알 궤적을 그림
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 탄알 궤적을 지움
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        if(state == State.Reloading || ammoRemain == 0 || magAmmo >= gunData.magCapacity)
        {
            return false;
        }
        StartCoroutine(ReloadRoutine());
        return true;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;

        //재장전 소리 재생
        gunAudioPlayer.PlayOneShot(gunData.reloadClip);
        // 재장전 소요 시간 만큼 처리 쉬기
        yield return new WaitForSeconds(gunData.reloadTime);

        //재장전시 채울 탄환 계산
        int ammoToFill = gunData.magCapacity - magAmmo;

        //총알이 탄창보다 덜 남았을때 재장전시
        if(ammoRemain < ammoToFill)
        {
            ammoToFill = ammoRemain;
        }

        //삽탄
        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}