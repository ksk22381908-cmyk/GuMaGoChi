# Windows 배포 코드 서명

릴리스 워크플로는 Windows 빌드·테스트 후 Microsoft Artifact Signing으로 EXE에 SHA-256 Authenticode 서명과 RFC 3161 타임스탬프를 추가합니다. Windows의 서명 신뢰 검증에 성공한 EXE만 패키징합니다. ZIP 체크섬은 서명 이후에 계산합니다. 인증이 없거나 서명 검증이 실패하면 릴리스를 게시하지 않습니다.

## 최초 설정

1. Azure에서 Microsoft Artifact Signing 계정을 만들고 신원 확인을 완료한 뒤 **Public Trust** 인증서 프로필을 만듭니다. 공개 배포에는 Private Trust 또는 Self-Signed 프로필을 사용하지 않습니다. 서비스 요금과 신청 가능 지역·개인/사업자 요건은 Azure의 현재 안내를 확인하세요.
2. Microsoft Entra 앱 등록 또는 사용자 할당 관리 ID를 만들고, 해당 ID에 서명 프로필 범위의 **Artifact Signing Certificate Profile Signer** 역할을 부여합니다.
3. GitHub 저장소 Settings → Environments에서 `release-signing` 환경을 만듭니다.
4. Azure ID의 GitHub OIDC 연합 자격 증명을 구성합니다. Issuer는 `https://token.actions.githubusercontent.com`, Audience는 `api://AzureADTokenExchange`, Subject는 `repo:ksk22381908-cmyk/GuMaGoChi:environment:release-signing`입니다.
5. `release-signing` 환경의 Variables에 다음 값을 등록합니다. ID와 리소스 이름은 비밀 키가 아니며, 클라이언트 비밀·PFX·인증서 개인 키는 사용하지 않습니다.

| Variable | 값 |
| --- | --- |
| `AZURE_CLIENT_ID` | 앱 또는 관리 ID의 클라이언트 ID |
| `AZURE_TENANT_ID` | Azure 테넌트 ID |
| `AZURE_SUBSCRIPTION_ID` | Azure 구독 ID |
| `SIGNING_ENDPOINT` | 서명 계정 리전의 공식 HTTPS 엔드포인트 |
| `SIGNING_ACCOUNT_NAME` | Artifact Signing 계정 이름 |
| `SIGNING_CERTIFICATE_PROFILE` | Public Trust 프로필 이름 |

설정 후 새 버전 태그를 올리거나 Actions → Windows release → Run workflow에서 배포 태그를 선택하세요. 버전과 태그는 일치해야 하며 이미 게시된 릴리스 파일을 덮어쓰지 않습니다.

## 다운로드와 확인

서명 파일은 GitHub **Releases의 Windows ZIP**으로 배포합니다. 태그의 Source code ZIP에는 커밋 시점 EXE가 들어 있으며 CI가 추가한 서명이 반영되지 않습니다. 서명된 앱을 받으려면 Release ZIP을 다운로드하세요.

다운로드 후 전체 압축을 풀고 다음으로 확인할 수 있습니다.

```powershell
Get-AuthenticodeSignature .\GuMaGoChi.exe | Format-List Status,SignerCertificate,TimeStamperCertificate
```

`Status`가 `Valid`이고 게시자와 타임스탬프가 있는지 확인합니다. 개인 개발용 패키징은 기존 `./package.ps1` 명령을 사용할 수 있지만 서명된 공식 배포는 `-RequireSignature`를 사용합니다. 서명 후 EXE를 다시 빌드하거나 수정하면 서명이 사라지거나 무효가 됩니다.

공개 신뢰 코드 서명은 게시자와 파일 무결성을 확인하는 수단입니다. 새 앱의 SmartScreen 경고가 즉시 사라지는 것을 보장하지는 않습니다.

공식 설정 참고: https://github.com/Azure/artifact-signing-action/blob/main/docs/OIDC.md
