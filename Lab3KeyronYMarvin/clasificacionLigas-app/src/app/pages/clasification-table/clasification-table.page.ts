import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { 
  IonContent, 
  IonHeader, 
  IonTitle, 
  IonToolbar, 
  IonGrid, 
  IonRow, 
  IonCol, 
  IonSelect, 
  IonSelectOption, 
  IonSpinner, 
  IonCard, 
  IonCardContent, 
  IonCardHeader, 
  IonCardTitle,
  IonList,
  IonItem,
  IonLabel,
  IonAvatar,
  IonBadge
} from '@ionic/angular/standalone';
import { Preferences } from '@capacitor/preferences';
import { ClasificationService } from '../../services/clasification.service';
import { ILeague } from '../../models/league.model';
import { ISeason } from '../../models/season.model';
import { IClasification } from '../../models/clasification.model';

@Component({
  selector: 'app-clasification-table',
  templateUrl: './clasification-table.page.html',
  styleUrls: ['./clasification-table.page.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonContent,
    IonHeader,
    IonTitle,
    IonToolbar,
    IonGrid,
    IonRow,
    IonCol,
    IonSelect,
    IonSelectOption,
    IonSpinner,
    IonCard,
    IonCardContent,
    IonCardHeader,
    IonCardTitle,
    IonList,
    IonItem,
    IonLabel,
    IonAvatar,
    IonBadge
  ]
})
export class ClasificationTablePage implements OnInit {
  leagues: ILeague[] = [];
  seasons: ISeason[] = [];
  tableData: IClasification[] = [];
  selectedLeagueId: string = '';
  selectedSeason: string = '';
  isLoading: boolean = false;
  errorMessage: string = '';

  constructor(private clasificationService: ClasificationService) {}

  ngOnInit() {
    this.loadLeagues();
  }

  loadLeagues() {
    this.isLoading = true;
    this.clasificationService.getFootballLeagues().subscribe({
      next: async (leagues) => {
        this.leagues = leagues;
        if (this.leagues.length > 0) {
          // Check if there is a saved league selection
          const { value: savedLeagueId } = await Preferences.get({ key: 'last_selected_league' });
          const leagueExists = this.leagues.some(l => l.idLeague === savedLeagueId);
          if (savedLeagueId && leagueExists) {
            this.selectedLeagueId = savedLeagueId;
          } else {
            this.selectedLeagueId = this.leagues[0].idLeague;
            await Preferences.set({ key: 'last_selected_league', value: this.selectedLeagueId });
          }
          this.loadSeasons(this.selectedLeagueId);
        } else {
          this.isLoading = false;
        }
      },
      error: (err) => {
        this.errorMessage = 'Error al cargar las ligas';
        this.isLoading = false;
      }
    });
  }

  async onLeagueChange(event: any) {
    const leagueId = event.detail.value;
    if (leagueId) {
      this.selectedLeagueId = leagueId;
      this.tableData = [];
      await Preferences.set({ key: 'last_selected_league', value: leagueId });
      this.loadSeasons(leagueId);
    }
  }

  loadSeasons(leagueId: string) {
    this.isLoading = true;
    this.clasificationService.getSeasons(leagueId).subscribe({
      next: async (seasons) => {
        this.seasons = seasons;
        if (this.seasons.length > 0) {
          // Check if there is a saved season selection
          const { value: savedSeason } = await Preferences.get({ key: 'last_selected_season' });
          const seasonExists = this.seasons.some(s => s.strSeason === savedSeason);
          if (savedSeason && seasonExists) {
            this.selectedSeason = savedSeason;
          } else {
            this.selectedSeason = this.seasons[0].strSeason;
            await Preferences.set({ key: 'last_selected_season', value: this.selectedSeason });
          }
          this.loadTable(this.selectedLeagueId, this.selectedSeason);
        } else {
          this.isLoading = false;
        }
      },
      error: (err) => {
        this.errorMessage = 'Error al cargar las temporadas';
        this.isLoading = false;
      }
    });
  }

  async onSeasonChange(event: any) {
    const season = event.detail.value;
    if (season) {
      this.selectedSeason = season;
      await Preferences.set({ key: 'last_selected_season', value: season });
      this.loadTable(this.selectedLeagueId, this.selectedSeason);
    }
  }

  loadTable(leagueId: string, season: string) {
    this.isLoading = true;
    this.errorMessage = '';
    this.clasificationService.getTableClasification(leagueId, season).subscribe({
      next: (data) => {
        this.tableData = data;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = 'Error al cargar la clasificación';
        this.isLoading = false;
      }
    });
  }

  getZoneClass(description: string): string {
    if (!description) return '';
    const desc = description.toLowerCase();
    if (desc.includes('champions league') || desc.includes('championsleague') || desc.includes('championship')) {
      return 'zone-champions';
    }
    if (desc.includes('europa league') || desc.includes('europaleague')) {
      return 'zone-europa';
    }
    if (desc.includes('conference league') || desc.includes('conferenceleague')) {
      return 'zone-conference';
    }
    if (desc.includes('relegation') || desc.includes('descenso')) {
      return 'zone-relegation';
    }
    return '';
  }

  getFormArray(form: string): string[] {
    if (!form) return [];
    return form.split('');
  }
}
