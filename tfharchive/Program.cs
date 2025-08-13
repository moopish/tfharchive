using tfharchive.archive;

foreach (var item in Archive.Load("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Terminal Velocity\\Terminal Velocity\\CDROM.POD").GetAllImages())
{
    Console.WriteLine(item.Name);
}
;