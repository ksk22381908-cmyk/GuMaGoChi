using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace GuMaGoChi {
    public sealed class UpdateRelease {
        public string tag_name,body,html_url;public bool draft,prerelease;public UpdateAsset[] assets;
    }
    public sealed class UpdateAsset {public string name,browser_download_url;public long size;}
    public sealed class UpdateDelta {public int format;public string version,baseVersion;public UpdateFile[] required;}
    public sealed class UpdateFile {public string path,sha256;public long size;}
    public static class Updates {
        public const string Repository="https://github.com/ksk22381908-cmyk/GuMaGoChi";
        public const string Api="https://api.github.com/repos/ksk22381908-cmyk/GuMaGoChi/releases/latest";
        public const long MaxDownload=250L*1024*1024,MaxExpanded=600L*1024*1024;
        public static Version Current {get{return typeof(Updates).Assembly.GetName().Version;}}
        public static string CurrentText {get{return Current.ToString(3);}}
        public static Version ReleaseVersion(UpdateRelease release){Version version;if(release==null||release.draft||release.prerelease||String.IsNullOrEmpty(release.tag_name)||!Version.TryParse(release.tag_name.TrimStart('v'),out version))throw new InvalidDataException("올바른 정식 릴리즈가 아닙니다.");return new Version(version.Major,version.Minor,Math.Max(0,version.Build),Math.Max(0,version.Revision));}
        public static UpdateRelease Parse(string json){var release=new JavaScriptSerializer().Deserialize<UpdateRelease>(json);ReleaseVersion(release);return release;}
        public static WebClient Client(){ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;var client=new WebClient();client.Headers[HttpRequestHeader.UserAgent]="GuMaGoChi/"+CurrentText;client.Headers[HttpRequestHeader.Accept]="application/vnd.github+json";client.Encoding=System.Text.Encoding.UTF8;return client;}
        public static UpdateAsset Asset(UpdateRelease release,bool checksum){string name="GuMaGoChi-"+ReleaseVersion(release).ToString(3)+"-windows.zip"+(checksum?".sha256":"");var asset=(release.assets??new UpdateAsset[0]).SingleOrDefault(a=>a.name==name);string prefix=Repository+"/releases/download/"+release.tag_name+"/";if(asset==null||asset.size<=0||asset.size>(checksum?4096:MaxDownload)||asset.browser_download_url!=prefix+name)throw new InvalidDataException("검증 가능한 Windows 업데이트 파일이 없습니다.");return asset;}
        public static UpdateAsset NamedAsset(UpdateRelease release,string name,long limit,bool optional=false){var asset=(release.assets??new UpdateAsset[0]).SingleOrDefault(a=>a.name==name);if(asset==null&&optional)return null;if(asset==null||asset.size<=0||asset.size>limit||asset.browser_download_url!=Repository+"/releases/download/"+release.tag_name+"/"+name)throw new InvalidDataException("패치 파일 정보가 올바르지 않습니다.");return asset;}
        public static string DeltaName(UpdateRelease release){return "GuMaGoChi-"+ReleaseVersion(release).ToString(3)+"-delta";}
        public static bool CanUseDelta(string json,UpdateRelease release,string target){
            var parser=new JavaScriptSerializer {MaxJsonLength=8*1024*1024};var delta=parser.Deserialize<UpdateDelta>(json);Version baseline;
            if(delta==null||delta.format!=1||delta.version!=ReleaseVersion(release).ToString(3)||!Version.TryParse(delta.baseVersion,out baseline)||baseline>=ReleaseVersion(release)||delta.required==null||delta.required.Length>20000)throw new InvalidDataException("패치 목록이 올바르지 않습니다.");
            var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);bool matches=true;long size=0;
            foreach(var file in delta.required){if(file==null||!AllowedFile(file.path)||!seen.Add(file.path)||file.size<0||file.sha256==null||file.sha256.Length!=64||file.sha256.Any(c=>!Uri.IsHexDigit(c)))throw new InvalidDataException("패치 기준 파일이 올바르지 않습니다.");string path=SafePath(target,file.path);RejectLinks(target,path);size+=file.size;if(size>MaxExpanded)throw new InvalidDataException("패치 용량 제한을 초과했습니다.");if(!File.Exists(path)||new FileInfo(path).Length!=file.size){matches=false;continue;}using(var stream=File.OpenRead(path))using(var sha=SHA256.Create()){string hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");if(!hash.Equals(file.sha256,StringComparison.OrdinalIgnoreCase))matches=false;}}
            return matches;
        }
        public static void VerifyChecksum(string archive,string text){string[] parts=(text??"").Split(new[]{' ','\r','\n','\t'},StringSplitOptions.RemoveEmptyEntries);if(parts.Length!=2||parts[0].Length!=64||parts[0].Any(c=>!Uri.IsHexDigit(c))||parts[1].TrimStart('*')!=Path.GetFileName(archive))throw new InvalidDataException("업데이트 검증 파일이 올바르지 않습니다.");string hash;using(var stream=File.OpenRead(archive))using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");if(!hash.Equals(parts[0],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("업데이트 파일의 SHA-256이 일치하지 않습니다. 다시 다운로드해 주세요.");}
        public static bool AllowedFile(string relative){return relative=="GuMaGoChi.exe"||relative=="GuMaGoChi.exe.config"||relative.StartsWith("assets/",StringComparison.Ordinal);}
        public static string SafePath(string root,string relative){if(String.IsNullOrWhiteSpace(relative)||relative.Contains(":")||relative.StartsWith("/")||relative.StartsWith("\\")||relative.Split('/','\\').Any(p=>p==".."||p=="."))throw new InvalidDataException("업데이트 파일 경로가 올바르지 않습니다.");string fullRoot=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;string full=Path.GetFullPath(Path.Combine(fullRoot,relative.Replace('/',Path.DirectorySeparatorChar)));if(!full.StartsWith(fullRoot,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("업데이트 폴더 밖의 파일은 허용하지 않습니다.");return full;}
        static void RejectLinks(string root,string path){string current=path;string boundary=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);while(current!=null&&current.StartsWith(boundary,StringComparison.OrdinalIgnoreCase)){if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException("링크로 연결된 설치 경로는 업데이트할 수 없습니다.");if(current.Equals(boundary,StringComparison.OrdinalIgnoreCase))break;current=Path.GetDirectoryName(current);}}
        public static void Extract(string archive,string payload,Version expected,bool partial=false){
            Directory.CreateDirectory(payload);long expanded=0;var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);string prefix="GuMaGoChi-"+expected.ToString(3)+"-windows/";
            using(var zip=ZipFile.OpenRead(archive)){
                if(zip.Entries.Count>20000)throw new InvalidDataException("업데이트 파일 수가 너무 많습니다.");
                foreach(var entry in zip.Entries){string name=entry.FullName.Replace('\\','/');if(!name.StartsWith(prefix,StringComparison.Ordinal))throw new InvalidDataException("업데이트 ZIP의 폴더 구성이 올바르지 않습니다.");string relative=name.Substring(prefix.Length);if(relative.Length==0)continue;string destination=SafePath(payload,relative);if(name.EndsWith("/"))continue;if(!seen.Add(relative))throw new InvalidDataException("중복된 업데이트 파일입니다.");expanded+=entry.Length;if(expanded>MaxExpanded)throw new InvalidDataException("압축 해제 용량 제한을 초과했습니다.");if(!AllowedFile(relative))continue;RejectLinks(payload,destination);Directory.CreateDirectory(Path.GetDirectoryName(destination));using(var input=entry.Open())using(var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write)){byte[] buffer=new byte[65536];long written=0;int read;while((read=input.Read(buffer,0,buffer.Length))>0){written+=read;if(written>entry.Length||written>MaxExpanded)throw new InvalidDataException("압축 해제 용량이 올바르지 않습니다.");output.Write(buffer,0,read);}}}
            }
            var assembly=AssemblyName.GetAssemblyName(Path.Combine(payload,"GuMaGoChi.exe"));if(assembly.Name!="GuMaGoChi"||assembly.Version!=expected||(!partial&&(!File.Exists(Path.Combine(payload,"GuMaGoChi.exe.config"))||!Directory.Exists(Path.Combine(payload,"assets")))))throw new InvalidDataException("업데이트 실행 파일의 버전 또는 필수 파일이 올바르지 않습니다.");
        }
        public static void CheckWritable(string target){string probe=SafePath(target,".gumagochi-update-"+Guid.NewGuid().ToString("N"));RejectLinks(target,probe);try{using(var file=new FileStream(probe,FileMode.CreateNew,FileAccess.Write))file.WriteByte(0);}finally{if(File.Exists(probe))File.Delete(probe);}}
        static void AtomicCopy(string source,string destination){string temporary=Path.Combine(Path.GetDirectoryName(destination),".gumagochi-new-"+Guid.NewGuid().ToString("N"));try{File.Copy(source,temporary,false);if(File.Exists(destination))File.Replace(temporary,destination,null);else File.Move(temporary,destination);}finally{if(File.Exists(temporary))File.Delete(temporary);}}
        public static void Install(string payload,string target,Action<string,string> copy=null){
            payload=Path.GetFullPath(payload);target=Path.GetFullPath(target);if(Path.GetDirectoryName(target)==null||target.Equals(payload,StringComparison.OrdinalIgnoreCase)||payload.StartsWith(target.TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))throw new IOException("설치 폴더가 올바르지 않습니다.");
            if(!File.Exists(Path.Combine(target,"GuMaGoChi.exe")))throw new IOException("기존 GuMaGoChi.exe를 찾을 수 없습니다.");CheckWritable(target);
            string backup=Path.Combine(Path.GetDirectoryName(payload),"backup");var files=new List<string>();var existed=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(string source in Directory.GetFiles(payload,"*",SearchOption.AllDirectories)){string relative=source.Substring(payload.Length).TrimStart('\\','/').Replace('\\','/');if(!AllowedFile(relative))throw new InvalidDataException("교체할 수 없는 파일입니다.");string destination=SafePath(target,relative);RejectLinks(target,destination);files.Add(relative);if(File.Exists(destination)){existed.Add(relative);string saved=SafePath(backup,relative);Directory.CreateDirectory(Path.GetDirectoryName(saved));File.Copy(destination,saved,false);}}
            var changed=new List<string>();
            try{foreach(string relative in files.OrderBy(f=>f=="GuMaGoChi.exe"?1:0)){string destination=SafePath(target,relative);Directory.CreateDirectory(Path.GetDirectoryName(destination));if(copy!=null){changed.Add(relative);copy(SafePath(payload,relative),destination);}else{AtomicCopy(SafePath(payload,relative),destination);changed.Add(relative);}}}
            catch(Exception original){var failures=new List<Exception>();foreach(string relative in changed.AsEnumerable().Reverse()){try{string destination=SafePath(target,relative);if(existed.Contains(relative))AtomicCopy(SafePath(backup,relative),destination);else if(File.Exists(destination))File.Delete(destination);}catch(Exception ex){failures.Add(ex);}}if(failures.Count>0)throw new IOException("업데이트와 복구에 실패했습니다. 백업 위치: "+backup,new AggregateException(new[]{original}.Concat(failures)));throw new IOException("업데이트에 실패해 기존 파일로 복구했습니다.",original);}
        }
        static void RestoreInstalled(string payload,string target){string backup=Path.Combine(Path.GetDirectoryName(payload),"backup");foreach(string source in Directory.GetFiles(payload,"*",SearchOption.AllDirectories)){string relative=source.Substring(payload.Length).TrimStart('\\','/').Replace('\\','/');if(!AllowedFile(relative))throw new InvalidDataException("복구할 수 없는 파일입니다.");string destination=SafePath(target,relative),saved=SafePath(backup,relative);RejectLinks(target,destination);if(File.Exists(saved))AtomicCopy(saved,destination);else if(File.Exists(destination))File.Delete(destination);}}
        public static void Apply(string[] args){
            string target=null,payload=null;bool installed=false,canRestart=false;
            try{
                if(args.Length!=5)throw new ArgumentException("잘못된 업데이트 실행 요청입니다.");payload=Path.GetFullPath(args[1]);target=Path.GetFullPath(args[2]);int pid;if(!Int32.TryParse(args[3],out pid))throw new ArgumentException("잘못된 프로세스 번호입니다.");Version expected=Version.Parse(args[4]);
                try{using(var parent=Process.GetProcessById(pid)){if(!Path.GetFullPath(parent.MainModule.FileName).Equals(Path.Combine(target,"GuMaGoChi.exe"),StringComparison.OrdinalIgnoreCase))throw new IOException("종료 대기할 게임 프로세스가 일치하지 않습니다.");if(!parent.WaitForExit(30000))throw new IOException("게임 종료를 기다리는 시간이 초과됐습니다. 게임을 닫고 다시 시도해 주세요.");}}catch(ArgumentException){}
                if(AssemblyName.GetAssemblyName(Path.Combine(target,"GuMaGoChi.exe")).Name!="GuMaGoChi")throw new InvalidDataException("기존 게임 실행 파일이 올바르지 않습니다.");canRestart=true;
                if(AssemblyName.GetAssemblyName(Path.Combine(payload,"GuMaGoChi.exe")).Version!=expected)throw new InvalidDataException("설치할 버전이 일치하지 않습니다.");Install(payload,target);installed=true;
                Process.Start(new ProcessStartInfo(Path.Combine(target,"GuMaGoChi.exe")){WorkingDirectory=target});
            }catch(Exception ex){string message=ex.Message;try{if(installed){RestoreInstalled(payload,target);message+="\n이전 버전으로 복구했습니다.";}if(canRestart)Process.Start(new ProcessStartInfo(Path.Combine(target,"GuMaGoChi.exe")){WorkingDirectory=target});}catch(Exception recovery){message+="\n복구 또는 재실행 실패: "+recovery.Message+"\n백업 위치: "+Path.Combine(Path.GetDirectoryName(payload),"backup");}MessageBox.Show(message,"GuMaGoChi 업데이트",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        }
    }
}
