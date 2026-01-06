import { Connection } from "../connection";

export interface DisplayConnection extends Connection {
  consumerName: string;
  utilityName: string;
  tariffName: string;
}

