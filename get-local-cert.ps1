## From: https://gist.github.com/dmcruz/82e7f8ee368843729426988ab75f9a9f

$cert = New-SelfSignedCertificate -Type Custom -Subject "localhost" -DnsName "localhost", "127.0.0.1" -KeyAlgorithm RSA -KeyLength 2048 -KeyExportPolicy Exportable -CertStoreLocation "Cert:\CurrentUser\My"

#Do not actually use this password.
$password = ConvertTo-SecureString -String "Pa55w.rd" -AsPlainText -Force

Export-PfxCertificate -Cert $cert -FilePath ".\cert.pfx" -Password $password