import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { UserPicker } from './user-picker/user-picker';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, UserPicker],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {}
