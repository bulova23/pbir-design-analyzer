import { EventEmitter } from 'events';

export type LanguageClientOptions = any;
export type ServerOptions = any;
export enum State {
  Stopped = 0,
  Starting = 1,
  Running = 2,
}

export class LanguageClient {
  private state = State.Stopped;
  private stateEmitter = new EventEmitter();
  constructor(
    public id: string,
    public name: string,
    public serverOptions: ServerOptions,
    public clientOptions: LanguageClientOptions
  ) {}

  onDidChangeState(listener: (event: { newState: number; oldState?: number }) => void) {
    this.stateEmitter.on('state', listener);
  }

  isRunning() {
    return this.state === State.Running;
  }

  async stop() {
    this.state = State.Stopped;
    this.stateEmitter.emit('state', { newState: State.Stopped });
  }
}
