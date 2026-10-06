using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace GuMaGoChi {
    public static class UpdateTests {
        static int count;
        static void Check(bool condition,string label){if(!condition)throw new Exception("UPDATE FAIL: "+label);count++;}
        static void Rejected(Action action,string label){try{action();}catch(Exception ex){if(ex is InvalidDataException||ex is IOException){count++;return;}throw;}throw new Exception("UPDATE FAIL: "+label);}
        static void WriteEntry(ZipArchive zip,string name,byte[] bytes){using(var stream=zip.CreateEntry(name).Open())stream.Write(bytes,0,bytes.Length);}
        public static int Run(){
            count=0;string root=Path.Combine(Paths.BaseDirectory,"update-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            try{
                Check(Updates.ReleaseVersion(Updates.Parse("{\"tag_name\":\"v0.2.6\",\"assets\":[]}"))==new Version(0,2,6,0),"release version normalization");Rejected(()=>Updates.Parse("{\"tag_name\":\"v0.2.6-beta\"}"),"invalid tag");Rejected(()=>Updates.Parse("{\"tag_name\":\"v0.2.6\",\"prerelease\":true}"),"prerelease");
                var release=new UpdateRelease {tag_name="v0.2.6",assets=new[]{new UpdateAsset {name="GuMaGoChi-0.2.6-windows.zip",size=50,browser_download_url=Updates.Repository+"/releases/download/v0.2.6/GuMaGoChi-0.2.6-windows.zip"}}};Check(Updates.Asset(release,false).size==50,"official asset URL");release.assets[0].browser_download_url="https://example.com/malware.zip";Rejected(()=>Updates.Asset(release,false),"foreign download URL");
                foreach(string path in new[]{"../save.json","assets/../../save.json","/absolute","C:/outside","assets/file:stream","assets\\..\\escape"})Rejected(()=>Updates.SafePath(root,path),"unsafe path "+path);
                Check(!Updates.AllowedFile("save.json")&&!Updates.AllowedFile("README.md")&&Updates.AllowedFile("assets/a.png"),"only runtime files are replaceable");
                string archive=Path.Combine(root,"GuMaGoChi-"+Updates.CurrentText+"-windows.zip"),prefix="GuMaGoChi-"+Updates.CurrentText+"-windows/";
                using(var zip=ZipFile.Open(archive,ZipArchiveMode.Create)){WriteEntry(zip,prefix+"GuMaGoChi.exe",File.ReadAllBytes(typeof(Updates).Assembly.Location));WriteEntry(zip,prefix+"GuMaGoChi.exe.config",System.Text.Encoding.UTF8.GetBytes("config"));WriteEntry(zip,prefix+"assets/test.png",new byte[]{1,2,3});WriteEntry(zip,prefix+"save.json",new byte[]{9});}
                string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(archive))).Replace("-","");Updates.VerifyChecksum(archive,hash+"  "+Path.GetFileName(archive));count++;Rejected(()=>Updates.VerifyChecksum(archive,new string('0',64)+"  "+Path.GetFileName(archive)),"corrupt download");Rejected(()=>Updates.VerifyChecksum(archive,hash+"  wrong.zip"),"wrong checksum filename");
                string payload=Path.Combine(root,"success","payload"),target=Path.Combine(root,"installed");Updates.Extract(archive,payload,Updates.Current);Check(File.Exists(Path.Combine(payload,"assets/test.png"))&&!File.Exists(Path.Combine(payload,"save.json")),"extract runtime files without importing saves");
                Directory.CreateDirectory(Path.Combine(target,"assets"));File.WriteAllText(Path.Combine(target,"GuMaGoChi.exe"),"old exe");File.WriteAllText(Path.Combine(target,"save.json"),"keep save");File.WriteAllText(Path.Combine(target,"assets/test.png"),"old asset");Updates.Install(payload,target);Check(File.ReadAllBytes(Path.Combine(target,"GuMaGoChi.exe")).SequenceEqual(File.ReadAllBytes(typeof(Updates).Assembly.Location))&&File.ReadAllText(Path.Combine(target,"save.json"))=="keep save","install leaves save untouched");Check(File.ReadAllText(Path.Combine(root,"success/backup/GuMaGoChi.exe"))=="old exe","backup old executable");
                string bad=Path.Combine(root,"traversal.zip");using(var zip=ZipFile.Open(bad,ZipArchiveMode.Create))WriteEntry(zip,prefix+"assets/../../save.json",new byte[]{1});Rejected(()=>Updates.Extract(bad,Path.Combine(root,"bad"),Updates.Current),"ZIP traversal");
                string failedPayload=Path.Combine(root,"failure/payload"),failedTarget=Path.Combine(root,"previous");Directory.CreateDirectory(Path.Combine(failedPayload,"assets"));Directory.CreateDirectory(failedTarget);File.WriteAllText(Path.Combine(failedTarget,"GuMaGoChi.exe"),"previous exe");File.WriteAllText(Path.Combine(failedPayload,"GuMaGoChi.exe"),"next exe");File.WriteAllText(Path.Combine(failedPayload,"assets/new.png"),"new asset");int copies=0;Rejected(()=>Updates.Install(failedPayload,failedTarget,(source,destination)=>{File.Copy(source,destination,true);if(++copies==2)throw new IOException("simulated failure after partial overwrite");}),"rollback on copy failure");Check(File.ReadAllText(Path.Combine(failedTarget,"GuMaGoChi.exe"))=="previous exe"&&!File.Exists(Path.Combine(failedTarget,"assets/new.png")),"rollback restores replaced files and removes new files");
                var data=new SaveData {StarterNutrientsClaimed=true};data.Pets.Add(new Pet {Active=false});using(var app=new DesktopApp(data,null,false))using(var dialog=new UpdateWindow(app,false)){
                    dialog.StartPosition=FormStartPosition.Manual;dialog.Location=new Point(-2000,-2000);dialog.Show();Application.DoEvents();
                    foreach(int percent in new[]{100,50,200,100})dialog.ApplyUiZoom(percent);
                    var footer=dialog.Controls.OfType<FlowLayoutPanel>().Single();Check(footer.Controls.OfType<Button>().Count()==3,"update dialog offers install, retry and cancel");
                    foreach(var button in footer.Controls.OfType<Button>()){Size text=TextRenderer.MeasureText(button.Text,button.Font);Check(button.Width>=text.Width+button.Padding.Horizontal,"update button text fits");}
                    using(var image=new Bitmap(dialog.Width,dialog.Height)){dialog.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(Path.Combine(Paths.BaseDirectory,"update-dialog-preview.png"));}app.Exit();
                }
                return count;
            }finally{string full=Path.GetFullPath(root);if(full.StartsWith(Path.GetFullPath(Paths.BaseDirectory).TrimEnd('\\')+"\\",StringComparison.OrdinalIgnoreCase))Directory.Delete(full,true);}
        }
    }
}
