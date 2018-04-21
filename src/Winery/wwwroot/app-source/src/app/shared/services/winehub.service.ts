import { Injectable } from '@angular/core';
import { api } from '../../app.config';
import * as signalR from '@aspnet/signalr';

@Injectable()
export class WinehubService {
    private connection: signalR.HubConnection;
    private connectionPromise: Promise<void>;

    constructor() {
        this.connection = new signalR.HubConnection(api.wineHub);
        this.connection.on('send', this.send);
        this.connection.on('commandCompleted', this.commandCompleted);
        this.connection.onclose(() => { console.log("browser is closing connection."); setTimeout(() => this.connection.start(), 10000); })
        this.connectionPromise = this.connection.start();
    }

    private send(message): void {
        console.log(message);
    }

    public severSend(message: string): Promise<{}> {
        let send = () => this.connection.invoke('send', "Ebi is in the building");
        if (this.connectionPromise) return this.connectionPromise.then(send);
        else return send();
    }

    public commandCompleted(command, commandResult): void {
        console.log("Command Completed: ", command, commandResult);
    }
}
