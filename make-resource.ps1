cd "Resources"
..\7z.exe a -ttar -aoa -y "Resources.tar" "icons.png" "icons@2x.png" "icons-pos.csv" "offset.json" "status.exh_ko.csv"
$(CertUtil -hashfile Resources.tar MD5)[1] -replace " ","" > "Resources.tar.md5"