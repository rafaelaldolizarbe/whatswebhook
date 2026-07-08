variable "region" {
  description = "Região OCI"
  type        = string
  default     = "sa-saopaulo-1"
}

variable "compartment_id" {
  description = "OCID do compartment onde os recursos serão criados (tenancy raiz, já que não há sub-compartments)"
  type        = string
}

variable "ssh_public_key_path" {
  description = "Caminho da chave pública SSH usada para acessar a VM"
  type        = string
  default     = "~/.ssh/id_ed25519.pub"
}

variable "instance_display_name" {
  description = "Nome de exibição da instância na OCI"
  type        = string
  default     = "whatsapp-webhook"
}

# Always Free (Ampere A1.Flex): até 4 OCPUs / 24 GB no total, por tenancy/região.
# Deixamos folga (2/12) para outras VMs futuras na mesma conta.
variable "instance_ocpus" {
  description = "Quantidade de OCPUs da instância (shape flexível VM.Standard.A1.Flex)"
  type        = number
  default     = 2
}

variable "instance_memory_gb" {
  description = "Memória em GB da instância (shape flexível VM.Standard.A1.Flex)"
  type        = number
  default     = 12
}

variable "ssh_allowed_cidr" {
  description = "CIDR autorizado a acessar a porta 22 (SSH). Restrinja ao seu IP em produção."
  type        = string
  default     = "0.0.0.0/0"
}
