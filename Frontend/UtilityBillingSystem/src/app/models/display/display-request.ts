import { UtilityRequest } from "../utility-request";

export interface DisplayRequest extends UtilityRequest {
  consumerName: string;
  utilityName: string;
}

