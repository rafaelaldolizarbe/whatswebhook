output "instance_public_ip" {
  description = "IP público da VM — usar para configurar o DNS do subdomínio (registro A) e para SSH"
  value       = oci_core_instance.this.public_ip
}

output "ssh_command" {
  description = "Comando pra acessar a VM via SSH"
  value       = "ssh ubuntu@${oci_core_instance.this.public_ip}"
}
