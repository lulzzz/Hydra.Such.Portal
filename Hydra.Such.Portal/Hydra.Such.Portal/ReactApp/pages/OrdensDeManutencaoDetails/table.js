// https://github.com/diegohaz/arc/wiki/Atomic-Design
/*react-styleguide: ignore*/
import React, { Component } from 'react';
import axios from 'axios';
import CircularProgress from '@material-ui/core/CircularProgress';
import styled, { css, theme, injectGlobal, withTheme } from 'styled-components';
import _theme from '../../themes/default';
import { Button, Text, Icon, Circle, Wrapper, OmDatePicker, CheckBox, Input, Avatars, Modal, Tooltip, Spacer, Breadcrumb } from 'components';
import moment from 'moment';
import ReactDOM from 'react-dom';
import { createMuiTheme } from '@material-ui/core/styles';
import MuiTableCell from '@material-ui/core/TableCell';
import TableRow from '@material-ui/core/TableRow';
import Color from 'color';
import Highlighter from "react-highlight-words";
import MuiInput from '@material-ui/core/Input';
import InputAdornment from '@material-ui/core/InputAdornment';
import { renderToString } from 'react-dom/server';
import MuiGrid from '@material-ui/core/Grid';
import {
	Column,
	FilteringState, GroupingState,
	IntegratedFiltering, IntegratedGrouping, IntegratedPaging, IntegratedSelection, IntegratedSorting,
	PagingState, SelectionState, SortingState, DataTypeProvider, DataTypeProviderProps, CustomGrouping,
	TreeDataState, CustomTreeData, RowDetailState, VirtualTableState, SearchState
} from '@devexpress/dx-react-grid';

import {
	DragDropProvider,
	Grid as TGrid, PagingPanel,
	Table, TableFilterRow, TableGroupRow,
	TableHeaderRow, TableSelection, Toolbar, GroupingPanel, VirtualTable,
	TableColumnReordering, ColumnChooser, TableColumnVisibility, TableColumnResizing,
	SearchPanel, VirtualTableView
} from '@devexpress/dx-react-grid-material-ui';

axios.defaults.headers.post['Accept'] = 'application/json';
axios.defaults.headers.get['Accept'] = 'application/json';

const muiTheme = createMuiTheme();
const breakpoints = muiTheme.breakpoints.values;

injectGlobal`
    body {
        background-color: white;   
    }
    .navbar-container, .navbar-header {
        background-color: ${_theme.palette.secondary.default};
    }
    .app-main {
        .row {
            margin: 0;
        }
        .wrap {
            padding: 0;
        }
    }
    @keyframes fade {
        from { opacity: 1.0; }
        50% { opacity: 0.5; }
        to { opacity: 1.0; }
    }                                                                                                                                                                                                                                  

    @-webkit-keyframes fade {
        from { opacity: 1.0; }
        50% { opacity: 0.5; }
        to { opacity: 1.0; }
    }
    .blink {
        animation:fade 1000ms infinite;
        -webkit-animation:fade 1000ms infinite;
    }
    mark, .mark {
        background-color: ${_theme.palette.search} ;
        padding: 0;
    }
        [class*="MuiTableRow"] {
                .first-cell {
                        [class*="icon"] {
                                left: 24px !important;
                        }
                }
        }
        [class*="GroupPanelContainer"] {
                [class*="MuiChip-root"] {
                        position: relative;
                        background: ${_theme.palette.primary.default};
                        border-radius: 7px;
                        padding: 0;
                        color: white;
                        font-family: Inter,Helvetica,sans-serif;
                        font-style: normal;
                        font-weight: 400;
                        font-size: 12px;
                        line-height: 16px;
                        text-transform: uppercase;
                        margin: 0 5px;
                        height: 24px;
                        &:hover, &:active, &:focus {
                                background: ${_theme.palette.primary.default};
                                border-radius: 7px;
                                color: white;
                                font-family: Inter,Helvetica,sans-serif;
                                font-style: normal;
                                font-weight: 400;
                                font-size: 12px;
                                line-height: 16px;
                                text-transform: uppercase;
                                margin: 0 5px;
                        }
                        [class*="MuiButtonBase-root"] {
                                color: white;
                        }
                        [class*="MuiChip-label"] {
                                padding-left: 8px;
                                padding-right: 0px;
                        }
                        [class*="MuiChip-deleteIcon"] {
                                color: ${_theme.palette.primary.default};
                        }
                        &[class*="MuiChip-deletable"] {
                                &:before {
                                        font-family: 'eSuch' !important;
                                        content: '\\e90b';
                                        color: white;
                                        position: absolute;
                                        top: 4px;                                                                
                                        right: 4px;
                                        pointer-events: none;
                                }
                        }
                }
        }
        [class*="MuiPopover-paper"] {
                [class*="DragDrop-container"] {
                        [role="button"] {
                                background: ${_theme.palette.primary.default};
                                border-radius: 7px;
                                padding: 0;
                                color: white;
                                font-family: Inter,Helvetica,sans-serif;
                                font-style: normal;
                                font-weight: 400;
                                font-size: 12px;
                                line-height: 16px;
                                text-transform: uppercase;
                                margin: 0 5px;
                                height: 24px;
                                opacity: .3;
                        }
                }
                [class*="MuiSvgIcon-root"] {
                        &[focusable] {
                                color: ${_theme.palette.primary.default}
                        }
                }
                [class*="MuiTypography-root"] {
                        font-family: Inter,Helvetica,sans-serif;
                        font-style: normal;
                        font-weight: 400;
                        font-size: 14px;
                        line-height: 24px;
                        margin: 0;
                        color: #323F4B;
                }
        }
        .table--row--hoverable {
                cursor: pointer;
                &:hover {
                        background: ${_theme.palette.bg.grey};
                }
        }
        
`
const Grid = styled(MuiGrid)`
    position: relative;
`

const TableCell = styled(MuiTableCell)` && { 
        padding: 15px 24px 15px 24px;
        font-size: inherit;
        color: ${_theme.palette.primary.default};
        white-space: nowrap;
        text-overflow: ellipsis;
        border: none;
        max-width: 12vw;
        overflow: hidden;
        p {
            margin: 0;
        }
    }
`;

const PickerButton = styled(Button)` && {
        position: relative;
        z-index: 10;
        padding-left: 25px;
        padding-right: 25px;
    }
`;

const PullRight = styled.span`
    float: right;
`

const TbleIcon = styled(Icon)`
    font-size: 24px;
`

const ListContainer = styled.div`
    position: absolute;
    top:0;
    left:0;
    right:0;
    bottom: 0;
    z-index: 0;
    overflow: auto;
    padding: 0;
    [class*="RootBase-root"]{
            position: absolute;
            top: 0;
            bottom: 0;
    }
`
const Hr = styled.hr`
    margin-bottom: 0;
    margin-top: 0;
`;
//updateTechnicals({ orderId: "OM1901562", technicalsId: ["42664", "105590", "106624"] });

const ListGridRow = styled(Grid)`  
    padding: 16px 0px;
    cursor: pointer;
    &:hover {
        background-color: ${Color(_theme.palette.primary.keylines).rgb().fade(0.5).toString()};
    }
`

const ListGridItem = styled(Grid)`  
    padding: 10px 25px;
`

const ListGridItemTextOverflow = styled(ListGridItem)`
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    p {
        margin:0;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }
`

const TextDiv = styled(Text)``.withComponent('div');

const TextField = styled(MuiInput)` && {
        margin: 0;
        display: block;
        margin: 0 auto;
        display: block;
        &[class*="MuiInputBase-focused"] {
            width: 100%;
            button {
                right: 7px;
                color: ${_theme.palette.secondary.default};
                pointer-events: none;
            }
            &:before {
                border-bottom: 1px solid ${_theme.palette.primary.dark} !important;
            }
            &:after {
                border-bottom: 1px solid ${_theme.palette.primary.dark} !important;
            }
        }
        &:before {
                border-bottom: 0px solid ${_theme.palette.primary.dark} !important;
        }
        &:after {
                border-bottom: 1px solid ${_theme.palette.primary.dark} !important;
        }
        &:hover {
            &:before  {
                border-bottom: 0px solid ${_theme.palette.primary.dark} !important;
            }
        }
       
        input {
            font-family: ${_theme.fonts.primary};
            font-style: normal;
            font-weight: 400;
            font-size: 14px;
            line-height: 24px;
            height: auto;
            padding-left: 10px;
            padding-right: ${props => props.affix ? '40px' : '5px'};
            padding-top: 10px;
            padding-bottom: 10px;
            width: 100%;
            color: transparent;
            text-shadow: 0 0 0 ${props => Color(props.color || _theme.palette.primary.dark).opaquer(-0.1).rgb().toString()};
            letter-spacing: .02em;
            &:focus {
                transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1) .3s;
                color: ${props => props.color || _theme.palette.primary.dark};
                text-shadow: none;
            }
        }
    }
`;

const SearchButton = styled(Button)` && {
        transition: right 200ms cubic-bezier(0.4, 0, 0.2, 1) 0ms;
        background: none;
        position: absolute;
        right: 25px;
        top: 0px;
        color: ${_theme.palette.primary.default};
        [class*="icon"] {
            font-size: 27px;
        }
    }
`;

const SearchWrapper = styled.div`
    position: absolute;
    display: block;
    width: ${props => props.width || '50%'};
    top: 20px;
    right: 0;
    z-index: 2;
    padding: 0 25px 0;
`;

var sortTimer = 0;

const BoldFormatter = ({ value }) => <Text b >{value}</Text>;
const BoldTypeProvider = props => (
	<DataTypeProvider
		formatterComponent={BoldFormatter}
		{...props}
	/>
);
const getRowId = row => row.numEquipamento;

class eTable extends Component {
	state = {
		group: [],
		hiddenColumns: [],
		tooltipReady: false,
		lines: [],
		skip: 0,
		sort: {},
		searchValue: "",
		isLoading: false,
		rows: [],
		total: 0,
		page: 0
	}

	constructor(props) {
		super(props);
		moment.locale("pt");
		this.getInitials = this.getInitials.bind(this);
		this.handleGridScroll = this.handleGridScroll.bind(this);
		this.fetchNew = this.fetchNew.bind(this);
		this.fetchNext = this.fetchNext.bind(this);
		this.state.orderId = this.props.orderId;
		this.state.group = props.columns.filter((item) => !!item.defaultExpandedGroup).map((item) => { return { columnName: item.name } });
		this.state.isLoading = props.isLoading;
		this.state.rows = props.rows;
		console.log(this.state.group);
	}

	fetchNew({ search, sort }) {
		this.setState({ searchValue: search, sort, page: 0 }, () => {
			this.props.getRows({ search, sort, page: 0 });
		});
	}

	fetchNext() {
		this.setState({ page: this.state.page + 1 }, () => {
			console.log('bababbabaabb', this.state.page);
			this.props.getRows({ search: this.state.searchValue, sort: this.state.sort, page: this.state.page });
		});
	}

	componentDidMount() {
		//window.addEventListener("resize", this.handleResize);
	}

	handleGridScroll(e) {
		s
		setTimeout(() => {
			Tooltip.Hidden.hide();
			Tooltip.Hidden.rebuild();
		});
	}

	componentWillReceiveProps(nextProps) {
		if (nextProps.isLoading !== this.state.isLoading) {
			this.setState({ isLoading: nextProps.isLoading });
		}
		if (nextProps.rows !== this.state.rows) {
			this.setState({ rows: nextProps.rows, total: nextProps.total });
		}
	}

	getInitials(name) {
		var initials = name.match(/\b\w/g) || [];
		initials = ((initials.shift() || '') + (initials.pop() || '')).toUpperCase();
		return initials;
	}

	setDatePickerMarginTop() {
		if (window.outerWidth < 960) {
			var pickerButton = ReactDOM.findDOMNode(this.pickerButton);
			var pickerButtonTop = (pickerButton.getBoundingClientRect().top * 1) - 70;
			this.setState({ datePickerMarginTop: pickerButtonTop + 'px' });
		} else if (this.state.datePickerMarginTop != 0) {
			this.setState({ datePickerMarginTop: 0 });
		}
	}

	render() {
		const { isLoading, rows, total } = this.state;
		var columns = this.props.columns;
		var headColumns = _.differenceBy(columns, this.state.group, 'columnName');
		headColumns = headColumns.filter((val) => {
			return this.state.hiddenColumns.indexOf(val.name) < 0;
		});
		var firstColumn = headColumns[0];
		var defaultExpandedGroups = getDefaultExpandedGroups(rows, this.state.group);
		var totalToLoad = rows.length + defaultExpandedGroups.length + this.props.pageSize;
		var totalMax = this.state.total + defaultExpandedGroups.length;
		var totalRowCount = totalToLoad;
		if (totalToLoad > totalMax) {
			totalRowCount = totalMax;
		}
		if (isLoading && rows.length == 0) {
			totalRowCount = 100;
		}
		return (
			<div>
				<div style={{ height: '100%', width: '100%', textAlign: 'center', position: 'absolute', zIndex: 1 }} className={isLoading ? "" : "hidden"}>
					<CircularProgress style={{ position: 'relative', top: '55%', color: this.props.theme.palette.secondary.default }} />
				</div>

				<SearchWrapper width={"25%"}>
					<TextField inputProps={{ autoComplete: "off" }} id="oms-search"
						onChange={(e) => {
							let search = e.target.value.toLowerCase();
							this.setState({ searchValue: search }, () => {
								Tooltip.Hidden.hide();
								Tooltip.Hidden.rebuild();
								clearTimeout(sortTimer);
								sortTimer = setTimeout(() => {
									this.fetchNew({ search: this.state.searchValue, sort: this.state.sort });
									clearTimeout(sortTimer);
									sortTimer = setTimeout(() => {
										this.fetchNew({ search: this.state.searchValue, sort: this.state.sort });
									}, 50);
								}, 400);
							});
						}} type="search" margin="none"
						endAdornment={
							<InputAdornment position="end" onClick={() => { document.getElementById("oms-search").focus() }}>
								<SearchButton round boxShadow={"none"} ><Icon search /></SearchButton>
							</InputAdornment>
						}
					/>
				</SearchWrapper>

				<TGrid rows={rows} columns={columns} getRowId={(item) => item[this.props.rowId]} >
					<BoldTypeProvider for={['nome']} />
					<SortingState onSortingChange={(e) => {
						this.setState({ sort: e[0] }, () => {
							Tooltip.Hidden.hide();
							Tooltip.Hidden.rebuild();
							this.fetchNew({ sort: e[0], search: this.state.searchValue });
							setTimeout(() => {
								this.setState({ sort: e[0] }, () => {
									Tooltip.Hidden.hide();
									Tooltip.Hidden.rebuild();
									this.fetchNew({ sort: e[0], search: this.state.searchValue });
								});
							}, 100);
						});
					}} columnExtensions={columns} />
					<SelectionState />
					<GroupingState expandedGroups={defaultExpandedGroups} grouping={this.state.group}
						columnExtensions={columns.map((item) => { return { columnName: item.name, groupingEnabled: item.groupingEnabled } })}
						onGroupingChange={(group) => {
							this.setState({ group: group });
						}}
					/>
					<SearchState />
					<IntegratedFiltering /><IntegratedSorting /><IntegratedSelection /><IntegratedGrouping /><DragDropProvider />
					<VirtualTableState
						infiniteScrolling={false}
						loading={this.state.isLoading}
						totalRowCount={totalRowCount}
						pageSize={this.props.pageSize}
						skip={this.state.skip}
						getRows={this.fetchNext} />
					{/*  */}

					<VirtualTable
						estimatedRowHeight={56}
						height="auto"
						noDataCellComponent={(props) => {
							return <VirtualTable.NoDataCell {...props} style={{
								position: "absolute", textAlign: "center", top: "50%",
								width: "100%", border: "none", padding: "0"
							}} getMessage={() => "Sem Dados"} />
						}}
						columnExtensions={columns.map((item) => { return { columnName: item.name, width: item.width } })}
						rowComponent={(props) => { return <VirtualTable.Row {...props} className="table--row--hoverable" /> }}
						cellComponent={(props) => {
							return (<MuiTableCell {..._.omit(props, ['tableRow', 'tableColumn'])}
								style={{
									paddingLeft: (props.column.columnName == firstColumn.name ? '30px' : '8px'),
									paddingRight: '8px',
									paddingTop: '16px', paddingBottom: '15px',
									borderColor: this.props.theme.palette.primary.keylines,
									borderWidth: '1px',
									color: this.props.theme.palette.primary.default,
									whiteSpace: "nowrap",
									overflow: 'hidden',
									textOverflow: 'ellipsis'
								}}
							>{props.column.dataType == "bold" ?
								<Text b style={{ color: this.props.theme.palette.primary.default }}
									data-html={true} data-tip={renderToString(
										<Highlighter searchWords={this.state.searchValue.split(" ")} autoEscape={true} textToHighlight={props.value || ""}></Highlighter>
									)}
								>
									<Highlighter searchWords={this.state.searchValue.split(" ")} autoEscape={true} textToHighlight={props.value || ""}></Highlighter>
								</Text>
								:
								<Text p style={{ color: this.props.theme.palette.primary.default }}
									data-html={true} data-tip={renderToString(
										<Highlighter searchWords={this.state.searchValue.split(" ")} autoEscape={true} textToHighlight={props.value || ""}></Highlighter>
									)}
								>
									<Highlighter searchWords={this.state.searchValue.split(" ")} autoEscape={true} textToHighlight={props.value || ""}></Highlighter>
								</Text>
								}
							</MuiTableCell>)
						}}
					/>

					<TableHeaderRow
						titleComponent={(props) => { return <Text label {...props} style={{ fontWeight: 500, marginTop: '6px' }} title="" data-tip={props.children} >{props.children}</Text> }}
						sortLabelComponent={(props) => {
							if (props.column.sortingEnabled == false) { return <TableHeaderRow.SortLabel {...props} onSort={() => { }} getMessage={() => ""} />; }
							return <TableHeaderRow.SortLabel  {...props} getMessage={() => ""} onSort={(e) => {
								console.log(e, props.onSort);
								this.setState({

								});
								props.onSort(e);
							}} />
						}}
						rowComponent={(props) => { return (<TableRow {..._.omit(props, ['tableRow'])} onMouseOver={() => ""} style={{ background: this.props.theme.palette.bg.grey }} />) }}
						cellComponent={(props) => {
							return (<TableHeaderRow.Cell {...props}

								style={{
									paddingLeft: props.groupingEnabled ? (props.column.columnName == firstColumn.name ? '48px' : '24px') : (props.column.columnName == firstColumn.name ? '28px' : '8px'),
									position: 'relative',
								}} className={(props.column.columnName == firstColumn.name ? 'first-cell' : '') +
									(props.groupingEnabled ? 'grouping-enabled' : '')} />)
						}}
						groupButtonComponent={(props) => {
							if (props.disabled) {
								return '';
							}
							return (<Icon observation onClick={props.onGroup}
								style={{ position: 'absolute', left: 0, paddingLeft: '0', paddingBottom: '2px', fontSize: '22px' }} />)
						}}
						showGroupingControls showSortingControls
					/>
					<TableGroupRow indentColumnWidth={1} showColumnsWhenGrouped={false}
						rowComponent={(props) => {
							var lastGroup = this.state.group[this.state.group.length - 1];
							if (lastGroup.columnName != props.row.groupedBy) { return (<tr></tr>); }
							var values = props.row.compoundKey.split('|');
							props.row.value = props.row.value;
							if (values.length == 1 && values[0] == "undefined") {
								values[0] = " ";
							}
							return (<TableRow {..._.omit(props, ['tableRow'])} style={{
								background: this.props.theme.palette.primary.default,
								paddingLeft: '8px', paddingRight: '8px',
								paddingTop: '16px', paddingBottom: '15px'
							}}>
								<MuiTableCell colSpan={props.children.length} key={props.tableRow.key.trim()}
									style={{
										paddingLeft: '30px', paddingRight: '8px',
										paddingTop: '16px', paddingBottom: '15px',
										color: 'white'
									}}
								>
									<Wrapper inline style={{ verticalAlign: 'middle', opacity: 0.5 }} padding="0 15px 0 0"><Icon equipamentos /></Wrapper>
									{values.map((value, index) => {
										return (<span key={index + props.tableRow.key.trim()} >{index > 0 ? <Icon arrow-right style={{ color: 'white', verticalAlign: 'middle', margin: '0 6px' }} /> : ''}
											<Text b key={index} style={{ color: 'white', verticalAlign: 'middle' }}
												data-html={true} data-tip={renderToString(
													<Highlighter searchWords={this.state.searchValue.split(" ")} autoEscape={true} textToHighlight={value}></Highlighter>
												)}
											>
												<Highlighter searchWords={this.state.searchValue.split(" ")} autoEscape={true} textToHighlight={value}></Highlighter>
											</Text></span>);
									})}
									<Wrapper inline style={{ verticalAlign: 'middle', float: 'right', width: '60px', textAlign: 'center', cursor: 'pointer' }} ><Icon open /></Wrapper>
								</MuiTableCell>
							</TableRow>)
						}} />
					<Toolbar />
					<GroupingPanel
						showSortingControls showGroupingControls
						emptyMessageComponent={(props) => <GroupingPanel.EmptyMessage getMessage={() => "Arraste um cabeçalho de coluna para agrupar."} />}
					/>
					<TableColumnVisibility hiddenColumnNames={this.state.hiddenColumns} onHiddenColumnNamesChange={(value) => {
						this.setState({ hiddenColumns: value });
					}} />
					<TableColumnReordering defaultOrder={columns.map(column => column.name)} />
					<ColumnChooser
						toggleButtonComponent={(props) => {
							return (<Button round style={{ top: '60px', 'zIndex': 1000, background: 'transparent', boxShadow: 'none', right: '-7px' }} {..._.omit(props, ['getMessage', 'active'])} onClick={props.onToggle}><Icon row-menu /></Button>);
						}}
						itemComponent={(props) => {
							if (props.item.column.selectionEnabled == false) { return ''; };
							return <ColumnChooser.Item {...props} />
						}}
					/>
					<RowDetailState defaultExpandedRowIds={true} />
				</TGrid>
				<Tooltip.Hidden id={'oms-tooltip'} />
			</div>
		)
	}
}

function multiGroupBy(array, group) {
	if (!group) {
		return array;
	}
	var currGrouping = _.groupBy(array, group);
	var restGroups = Array.prototype.slice.call(arguments);
	restGroups.splice(0, 2);
	if (!restGroups.length) {
		return currGrouping;
	}
	return _.transform(currGrouping, function (result, value, key) {
		result[key] = multiGroupBy.apply(null, [value].concat(restGroups));
	}, {});
}

var getDefaultExpandedGroups = (lines, groups) => {
	var defaultExpandedGroups = [];
	var groupedList = multiGroupBy(lines, ...groups.map((item) => { return item.columnName }));

	(function formatRecursively(list, referenceList, prefix) {
		if (prefix) { prefix = prefix + '|'; } else { prefix = ""; }
		var keys = _.keys(list);
		keys.map((item) => {
			var listItem = list[item];
			if (_.isObject(listItem) && !_.isArray(listItem)) { formatRecursively(listItem, referenceList, `${prefix}${item}`); }
			item = `${prefix}${item}`;
			referenceList.push(item);
		});
	})(groupedList, defaultExpandedGroups);

	return defaultExpandedGroups;
}

const mapStateToProps = state => ({
	...state
})
const mapDispatchToProps = dispatch => ({
	dispatchState: (payload) => dispatch({
		type: "SET_STATE",
		payload: payload
	})
})
export default withTheme(eTable);