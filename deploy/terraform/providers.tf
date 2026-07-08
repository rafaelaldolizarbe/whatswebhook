terraform {
  required_version = ">= 1.5"

  required_providers {
    oci = {
      source  = "oracle/oci"
      version = ">= 5.0"
    }
  }
}

# Autenticação via ~/.oci/config (perfil DEFAULT), já configurado nesta máquina.
provider "oci" {
  region = var.region
}
