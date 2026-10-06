using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace GuMaGoChi {
    public sealed class UpdateWindow:GameForm {
        readonly DesktopApp app;readonly Label versions,status;readonly TextBox notes;readonly ProgressBar progress;readonly Button check,install,close;
        WebClient client;UpdateRelease release;UpdateAsset archiveAsset,checksumAsset;bool busy,closed;string stage,archive,checksum;
        public UpdateWindow(DesktopApp owner):this(owner,true){}
        internal UpdateWindow(DesktopApp owner,bool autoCheck){
            app=owner;Text="GuMaGoChi 업데이트";ClientSize=new Size(560,440);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;BackColor=Art.Cream;Font=new Font("맑은 고딕",10);
            versions=new Label {Bounds=new Rectangle(20,18,520,48),Text="현재 버전  v"+Updates.CurrentText};Controls.Add(versions);
            notes=new TextBox {Bounds=new Rectangle(20,75,520,210),Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=Color.White,Text="최신 릴리즈를 확인하고 있습니다."};Controls.Add(notes);
            status=new Label {Bounds=new Rectangle(20,298,520,40),Text="업데이트 확인 중…"};Controls.Add(status);
            progress=new ProgressBar {Bounds=new Rectangle(20,341,520,18),Style=ProgressBarStyle.Marquee};Controls.Add(progress);
            check=UiLayout.DialogButton("다시 확인");check.Enabled=false;check.Click+=(s,e)=>Check();install=UiLayout.DialogButton("다운로드 및 설치");install.Enabled=false;install.Click+=(s,e)=>Download();close=UiLayout.DialogButton("닫기");close.Click+=(s,e)=>Close();Controls.Add(UiLayout.DialogActions(install,check,close));CancelButton=close;
            if(autoCheck)Shown+=(s,e)=>Check();FormClosed+=(s,e)=>{closed=true;if(client!=null){client.CancelAsync();client.Dispose();}if(app.Update==this)app.Update=null;};
        }
        void Begin(string message){busy=true;check.Enabled=install.Enabled=false;status.Text=message;progress.Style=ProgressBarStyle.Marquee;close.Text="취소";}
        void Failure(Exception error){if(closed)return;busy=false;check.Enabled=true;install.Enabled=release!=null&&archiveAsset!=null;progress.Style=ProgressBarStyle.Continuous;progress.Value=0;status.Text="업데이트 실패: "+error.Message;close.Text="닫기";}
        void NewClient(){if(client!=null)client.Dispose();client=Updates.Client();}
        void Check(){
            if(busy)return;release=null;archiveAsset=checksumAsset=null;Begin("GitHub 최신 버전 확인 중…");
            try{NewClient();client.DownloadStringCompleted+=(s,e)=>{
                if(closed)return;if(e.Cancelled){Failure(new OperationCanceledException("확인이 취소됐습니다."));return;}if(e.Error!=null){Failure(e.Error);return;}
                try{var candidate=Updates.Parse(e.Result);versions.Text="현재 버전  v"+Updates.CurrentText+"\n최신 버전  "+candidate.tag_name;notes.Text=candidate.body??"";bool newer=Updates.ReleaseVersion(candidate)>Updates.Current;if(newer){archiveAsset=Updates.Asset(candidate,false);checksumAsset=Updates.Asset(candidate,true);release=candidate;}busy=false;check.Enabled=true;install.Enabled=newer;progress.Style=ProgressBarStyle.Continuous;progress.Value=0;status.Text=newer?"새 버전을 설치할 수 있습니다. 저장 데이터는 유지됩니다.":"현재 최신 버전입니다.";close.Text="닫기";}catch(Exception ex){Failure(ex);}
            };client.DownloadStringAsync(new Uri(Updates.Api));}catch(Exception ex){Failure(ex);}
        }
        void Download(){
            if(busy||release==null)return;
            if(MessageBox.Show(this,"게임을 저장하고 종료한 뒤 업데이트를 설치하고 다시 실행합니다.\n계속할까요?","GuMaGoChi 업데이트",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
            try{Updates.CheckWritable(Paths.BaseDirectory);stage=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"GuMaGoChi-Updater",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);archive=Path.Combine(stage,archiveAsset.name);Begin("검증 파일 다운로드 중…");NewClient();client.DownloadStringCompleted+=(s,e)=>{
                if(closed)return;if(e.Cancelled||e.Error!=null){Failure(e.Error??new OperationCanceledException("다운로드가 취소됐습니다."));return;}checksum=e.Result;DownloadArchive();
            };client.DownloadStringAsync(new Uri(checksumAsset.browser_download_url));}catch(Exception ex){Failure(ex);}
        }
        void DownloadArchive(){
            try{NewClient();status.Text="업데이트 다운로드 중…";progress.Style=ProgressBarStyle.Continuous;client.DownloadProgressChanged+=(s,e)=>{if(closed)return;if(e.BytesReceived>Updates.MaxDownload){client.CancelAsync();return;}progress.Value=Math.Min(100,Math.Max(0,e.ProgressPercentage));status.Text="다운로드 "+e.ProgressPercentage+"% · "+(e.BytesReceived/1024/1024)+" MB";};client.DownloadFileCompleted+=(s,e)=>{if(closed)return;if(e.Cancelled||e.Error!=null){Failure(e.Error??new OperationCanceledException("다운로드가 취소됐거나 용량 제한을 초과했습니다."));return;}status.Text="파일 검증 및 압축 해제 중…";progress.Style=ProgressBarStyle.Marquee;Prepare();};client.DownloadFileAsync(new Uri(archiveAsset.browser_download_url),archive);}catch(Exception ex){Failure(ex);}
        }
        void OnUi(Action action){if(closed||IsDisposed||!IsHandleCreated)return;try{BeginInvoke((MethodInvoker)(()=>{if(!closed)action();}));}catch(InvalidOperationException){}}
        void Prepare(){
            ThreadPool.QueueUserWorkItem(state=>{
                try{if(new FileInfo(archive).Length!=archiveAsset.size)throw new InvalidDataException("다운로드 파일 크기가 일치하지 않습니다.");Updates.VerifyChecksum(archive,checksum);string payload=Path.Combine(stage,"payload");Version version=Updates.ReleaseVersion(release);Updates.Extract(archive,payload,version);
                    string helper=Path.Combine(stage,"GuMaGoChi-updater.exe");File.Copy(Path.Combine(Paths.BaseDirectory,"GuMaGoChi.exe"),helper);File.Copy(Path.Combine(Paths.BaseDirectory,"GuMaGoChi.exe.config"),helper+".config");
                    OnUi(()=>Install(helper,payload,version));
                }catch(Exception ex){OnUi(()=>Failure(ex));}
            });
        }
        void Install(string helper,string payload,Version version){
            try{if(!app.SaveForUpdate()){Failure(new IOException("게임 저장에 실패해 설치를 중단했습니다."));return;}string arguments="--apply-update \""+payload+"\" \""+Paths.BaseDirectory+"\" "+Process.GetCurrentProcess().Id+" "+version;
                var process=Process.Start(new ProcessStartInfo(helper,arguments){WorkingDirectory=stage,UseShellExecute=false,CreateNoWindow=true});if(process==null)throw new IOException("업데이터를 실행할 수 없습니다.");process.Dispose();app.Exit();
            }catch(Exception ex){Failure(ex);}
        }
    }
}
