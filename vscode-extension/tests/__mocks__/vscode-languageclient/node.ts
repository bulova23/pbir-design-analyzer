import { EventEmitter } from 'events';

export type LanguageClientOptions = any;
export type ServerOptions = any;

export class LanguageClient {
  private state = 0;
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
    return this.state === 1;
  }

  async stop() {
    this.state = 0;
    this.stateEmitter.emit('state', { newState: 0 });
  }
}
