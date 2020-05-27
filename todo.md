# To Do

* Whitelist Hyland IPs for dev/staging environments
  * This requires that PROXY Protocol is enabled on both the NLB and the nginx ingress controller
  * This is a setting in Terraform/the AWS console for the NLB target group

```terraform
resource "aws_lb_target_group" "tg" {
    ## Other configuration
    proxy_protocol_v2 = true
}
```

  * This is a config map change for nginx 

helm chart values.yaml
```yaml
controller:
  ## Other configuration 
  config:
    use-proxy-protocol: "true"
```

* Set the app pods to have the following annotation: 

```json
  "overrides": {
    "ingress.annotations.nginx\\.ingress\\.kubernetes\\.io/whitelist-source-range": "'${T(String).join(\"\\,\",#stage('Terraform Output - Development')['context']['status']['outputs']['hyland_ips']['value'])}'"
  }
```