"""Prepare supplied audio; preserve source files. Usage: script SOURCE_AUDIO_FOLDER."""
import array, math, pathlib, subprocess, sys, tempfile, wave
source=pathlib.Path(sys.argv[1])
dest=pathlib.Path(__file__).resolve().parents[2]/'Assets/Resources/HollowSFX'
clips={
'jump_0':('hero_jump',.48),'air_jump_0':('hero_jump',.48),
'dash_0':('hero_shade_dash_1',.42),'hurt_0':('hero_damage',.8),
'cast_0':('hero_fireball',1),'land_0':('hero_land_soft',.25),
'swing_0':('sword_1',.28),'swing_1':('sword_2',.3),
'pickup_0':('spell_pickup_pickup',2),'boss_jump_0':('mawlek_jump',.7),
'boss_land_0':('false_knight_strike_ground',1.5),
'laser_charge_0':('mines_pink_laser_prepare',.67),'laser_fire_0':('radiance_laser_burst',1.3),
'slime_hit_0':('enemy_damage',.6),'slime_death_0':('enemy_death_sword',.8),
'player_death_0':('hero_death_v2',2),'shift_0':('dream_ghost_appear',.7),
'step_concrete_0':('hero_run_footsteps_stone',.2),
'step_wood_0':('hero_run_footsteps_bones',.2),'step_snow_0':('hero_run_footsteps_grass',.2)}
def ff(args):subprocess.run(['ffmpeg','-v','error','-y']+args,check=True)
report=['# Curated supplied Hollow Knight audio','','Original supplied game audio belongs to Team Cherry. Source recordings remain unchanged.','Prepared by Tools/Polish/PrepareHollowSfx.py: mono 44.1 kHz, measured gain, peak ceiling and short fade.','']
with tempfile.TemporaryDirectory() as temp:
 for cue,(name,length) in clips.items():
  raw=pathlib.Path(temp)/'raw.wav';segment=pathlib.Path(temp)/'segment.wav'
  ff(['-i',str(source/(name+'.wav')),'-ac','1','-ar','44100','-c:a','pcm_s16le',str(raw)])
  with wave.open(str(raw)) as w: samples=array.array('h',w.readframes(w.getnframes()));rate=w.getframerate()
  offset=0
  if cue.startswith('step_'):
   window=int(rate*.1);hop=int(rate*.02)
   start=max(range(0,max(1,len(samples)-window),hop),key=lambda a:sum(x*x for x in samples[a:a+window]))
   offset=max(0,start/rate-.035)
  ff(['-ss',str(offset),'-i',str(raw),'-t',str(length),str(segment)])
  with wave.open(str(segment)) as w: a=array.array('h',w.readframes(w.getnframes()));duration=w.getnframes()/w.getframerate()
  rms=math.sqrt(sum(x*x for x in a)/len(a))/32768;peak=max(abs(x) for x in a)/32768
  target=-29 if cue.startswith('step_') else -26 if cue.startswith('land') else -23
  gain=min(8,10**(target/20)/max(rms,1e-6),.85/max(peak,1e-6))
  ff(['-i',str(segment),'-af',f'volume={gain},afade=t=out:st={max(0,duration-.035)}:d=0.035','-c:a','pcm_s16le',str(dest/(cue+'.wav'))])
  report.append(f'- {cue}.wav ← {name}.wav; offset {offset:.3f}s, duration {duration:.3f}s, gain {20*math.log10(gain):+.1f} dB.')
(dest/'PROVENANCE.md').write_text('\n'.join(report)+'\n')
print(f'Prepared {len(clips)} sound cues')
