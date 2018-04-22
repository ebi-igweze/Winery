import { Injectable } from '@angular/core';
import { api } from '../../app.config';
import * as signalR from '@aspnet/signalr';
import { CommandResult } from '../../app.models';
import { Subject } from 'rxjs/Subject';

export type CommandKeys = 
    | "AddCategory"
    | "UpdateCategory"
    | "DeleteCategory"
    | "AddWine"
    | "UpdateWine"
    | "DeleteWine"
    | "AddUser"
    | "UpdateUser"
    | "AddUserRole"

@Injectable()
export class WinehubService {
    private connection: signalR.HubConnection;
    private connectionPromise: Promise<void>;

    private commmands: { [key:string]: Subject<CommandResult> } = {
        "AddCategory": new Subject<CommandResult>(),
        "UpdateCategory": new Subject<CommandResult>(),
        "DeleteCategory": new Subject<CommandResult>(),
        "AddWine": new Subject<CommandResult>(),
        "UpdateWine": new Subject<CommandResult>(),
        "DeleteWine": new Subject<CommandResult>(),
        "AddUser": new Subject<CommandResult>(),
        "UpdateUser": new Subject<CommandResult>(),
        "AddUserRole": new Subject<CommandResult>()
    }
    
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

    private commandCompleted = (command: CommandKeys, commandResult: CommandResult) => {
        let subject = this.commmands[command];
        if (subject) subject.next(commandResult);
        console.log("Command Completed: ", command, commandResult);
    }

    public on(key: CommandKeys): Subject<CommandResult> {
        return this.commmands[key]
    }
}
